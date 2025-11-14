using MusicGame.SelectMusic.Model;
using System;
using UnityEngine;
using UnityDebug = UnityEngine.Debug; // 解决Debug歧义

namespace MusicGame.Game
{
    [RequireComponent(typeof(Collider))]
    public class NoteController : MonoBehaviour
    {
        [HideInInspector] public NoteObject noteObject = new NoteObject();

        [Header("音效设置")]
        public AudioClip hitSound;
        public AudioClip slideSound;
        public AudioClip missSound;

        private float _musicStartTime;
        private float _moveSpeed;
        private float _targetY;
        private Action<int, float> _judgeCallback;
        private bool _isJudged = false;
        private AudioSource _audioSource;

        public void Initialize(
            Note noteData,
            float musicStart,
            float speed,
            float targetY,
            Action<int, float> callback
        )
        {
            if (noteData == null)
            {
                UnityDebug.LogError("[NoteController] 初始化失败: noteData为空");
                Destroy(gameObject);
                return;
            }

            if (speed <= 0)
            {
                UnityDebug.LogError("[NoteController] 初始化失败: 速度必须为正数");
                Destroy(gameObject);
                return;
            }

            noteObject = new NoteObject
            {
                gameObject = gameObject,
                collider = GetComponent<Collider>(),
                note = noteData,
                spawnPosition = transform.position,
                targetPosition = new Vector3(transform.position.x, targetY, transform.position.z)
            };

            _musicStartTime = musicStart;
            _moveSpeed = speed;
            _targetY = targetY;
            _judgeCallback = callback;
            _isJudged = false;

            if (noteObject.collider != null)
            {
                noteObject.collider.isTrigger = true;
                var rb = GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }

            InitAudioSource();
        }

        private void InitAudioSource()
        {
            _audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.playOnAwake = false;
        }

        private void Update()
        {
            if (_isJudged || noteObject.note == null) return;

            MoveNote();
            CheckJudgement();
        }

        private void MoveNote()
        {
            var newPosition = Vector3.MoveTowards(
                transform.position,
                noteObject.targetPosition,
                _moveSpeed * Time.deltaTime
            );
            transform.position = newPosition;
        }

        private void CheckJudgement()
        {
            float distanceToJudge = Mathf.Abs(transform.position.y - _targetY);

            if (distanceToJudge < 0.3f && CheckTrackKey(noteObject.note.x))
            {
                ProcessJudgement(true);
            }
            else if (transform.position.y < _targetY - 0.5f)
            {
                ProcessJudgement(false);
            }
        }

        private void ProcessJudgement(bool isHit)
        {
            if (_isJudged) return;
            _isJudged = true;

            float accuracy = isHit
                ? Mathf.Abs((Time.time - _musicStartTime) - noteObject.note.time)
                : 1f;

            PlayJudgementSound(isHit);
            _judgeCallback?.Invoke(noteObject.note.x - 1, accuracy);
            Destroy(gameObject);
        }

        private void PlayJudgementSound(bool isHit)
        {
            if (isHit)
            {
                if (noteObject.note.type == NoteType.Slider || noteObject.note.type == NoteType.Slide)
                    _audioSource.PlayOneShot(slideSound);
                else
                    _audioSource.PlayOneShot(hitSound);
            }
            else
            {
                _audioSource.PlayOneShot(missSound);
            }
        }

        private bool CheckTrackKey(int trackNumber)
        {
            return trackNumber switch
            {
                1 => Input.GetKeyDown(KeyCode.A),
                2 => Input.GetKeyDown(KeyCode.S),
                3 => Input.GetKeyDown(KeyCode.D),
                4 => Input.GetKeyDown(KeyCode.F),
                5 => Input.GetKeyDown(KeyCode.G),
                6 => Input.GetKeyDown(KeyCode.H),
                7 => Input.GetKeyDown(KeyCode.J),
                _ => false
            };
        }
    }

    [Serializable]
    public class NoteObject
    {
        public GameObject gameObject;
        public Collider collider;
        public Note note;
        public Vector3 spawnPosition;
        public Vector3 targetPosition;
    }
}
