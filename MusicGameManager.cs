using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace YourUniqueNamespace
{
    public class MusicGameManager : MonoBehaviour
    {
        [Header("游戏核心设置")]
        [SerializeField] private float noteSpeed = 5f;
        [SerializeField] private float judgeWindow = 0.1f;
        [SerializeField] private Transform noteSpawnPoint;
        [SerializeField] private Transform judgeLine;
        [SerializeField] private GameObject notePrefab;
        [SerializeField] private AudioSource hitSoundSource;
        [SerializeField] private AudioSource emptyHitSoundSource;

        [Header("轨道发光效果设置")]
        [SerializeField] private List<Renderer> trackRenderers = new List<Renderer>();
        [SerializeField] private Color trackNormalColor = Color.white;
        [SerializeField] private Color trackHitColor = Color.cyan;
        [SerializeField] private float glowDuration = 0.2f;

        [Header("7个轨道配置（3D位置偏移）")]
        [SerializeField] private float trackSpacing = 1.5f;
        [SerializeField] private Vector3 trackZOffset = Vector3.zero;

        [Header("键盘绑定（7个轨道）")]
        [SerializeField] private KeyCode track1Key = KeyCode.A;
        [SerializeField] private KeyCode track2Key = KeyCode.S;
        [SerializeField] private KeyCode track3Key = KeyCode.D;
        [SerializeField] private KeyCode track4Key = KeyCode.F;
        [SerializeField] private KeyCode track5Key = KeyCode.G;
        [SerializeField] private KeyCode track6Key = KeyCode.H;
        [SerializeField] private KeyCode track7Key = KeyCode.J;

        [Header("游戏控制按键")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private KeyCode restartKey = KeyCode.F5;

        private List<Note> activeNotes = new List<Note>();
        private bool isGameRunning = true;
        private bool isPaused = false;

        public enum JudgeResult { Perfect, Great, Good, Miss, Empty }

        private void Start()
        {
            // 初始化音效
            if (hitSoundSource != null)
                hitSoundSource.clip = Resources.Load<AudioClip>("Sounds/hit");

            if (emptyHitSoundSource != null)
                emptyHitSoundSource.clip = Resources.Load<AudioClip>("Sounds/empty_hit");

            // 初始化轨道颜色
            InitializeTrackColors();

            // 仅使用测试音符生成
            StartCoroutine(SpawnTestNotes());
        }

        // 初始化轨道颜色为默认色
        private void InitializeTrackColors()
        {
            if (trackRenderers.Count != 7)
            {
                UnityEngine.Debug.LogWarning("轨道渲染器数量不为7，请检查trackRenderers配置");
                return;
            }

            foreach (var renderer in trackRenderers)
            {
                if (renderer != null)
                    renderer.material.color = trackNormalColor;
            }
        }

        private void Update()
        {
            if (!isGameRunning) return;

            if (Input.GetKeyDown(pauseKey)) TogglePause();
            if (isPaused)
            {
                if (Input.GetKeyDown(restartKey)) RestartGame();
                return;
            }

            UpdateNotes();
            CheckTrackInputs();
        }

        private void CheckTrackInputs()
        {
            if (Input.GetKeyDown(track1Key)) TriggerTrackInput(0);
            if (Input.GetKeyDown(track2Key)) TriggerTrackInput(1);
            if (Input.GetKeyDown(track3Key)) TriggerTrackInput(2);
            if (Input.GetKeyDown(track4Key)) TriggerTrackInput(3);
            if (Input.GetKeyDown(track5Key)) TriggerTrackInput(4);
            if (Input.GetKeyDown(track6Key)) TriggerTrackInput(5);
            if (Input.GetKeyDown(track7Key)) TriggerTrackInput(6);
        }

        private void TriggerTrackInput(int targetTrackIndex)
        {
            StartCoroutine(GlowTrack(targetTrackIndex));

            Note closestNote = null;
            float minDistance = float.MaxValue;

            foreach (var note in activeNotes)
            {
                if (note.TrackIndex != targetTrackIndex) continue;

                float distance = Mathf.Abs(note.transform.position.y - judgeLine.position.y);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNote = note;
                }
            }

            if (closestNote != null)
            {
                float timeDiff = minDistance / noteSpeed;
                JudgeResult result = GetJudgeResult(timeDiff);
                OnNoteJudged(closestNote, result);
                activeNotes.Remove(closestNote);
            }
            else
            {
                OnEmptyInput(targetTrackIndex);
            }
        }

        private IEnumerator GlowTrack(int trackIndex)
        {
            if (trackIndex < 0 || trackIndex >= trackRenderers.Count) yield break;
            var renderer = trackRenderers[trackIndex];
            if (renderer == null) yield break;

            Material originalMat = renderer.material;
            Material tempMat = new Material(originalMat);

            tempMat.color = trackHitColor;
            renderer.material = tempMat;

            yield return new WaitForSeconds(glowDuration);

            tempMat.color = trackNormalColor;
            renderer.material = originalMat;
            Destroy(tempMat);
        }

        private JudgeResult GetJudgeResult(float timeDiff)
        {
            if (timeDiff < judgeWindow * 0.3f) return JudgeResult.Perfect;
            if (timeDiff < judgeWindow * 0.6f) return JudgeResult.Great;
            if (timeDiff < judgeWindow) return JudgeResult.Good;
            return JudgeResult.Miss;
        }

        private void OnNoteJudged(Note note, JudgeResult result)
        {
            note.OnJudged(result);
            PlayHitEffect(result);
            UpdateScore(result);
            UnityEngine.Debug.Log($"轨道 {note.TrackIndex + 1} 判定: {result} | 分数: {RuntimeData.Score}");
        }

        private void OnEmptyInput(int trackIndex)
        {
            PlayHitEffect(JudgeResult.Empty);
            UpdateScore(JudgeResult.Empty);
            UnityEngine.Debug.Log($"轨道 {trackIndex + 1} 空击（无音符）");
        }

        private void UpdateScore(JudgeResult result)
        {
            switch (result)
            {
                case JudgeResult.Miss:
                    RuntimeData.MissCount++;
                    RuntimeData.Combo = 0;
                    break;
                case JudgeResult.Empty:
                    RuntimeData.Score = Mathf.Max(0, RuntimeData.Score - 10);
                    break;
                default:
                    RuntimeData.HitCount++;
                    RuntimeData.Combo++;

                    float multiplier = result switch
                    {
                        JudgeResult.Perfect => 1.0f,
                        JudgeResult.Great => 0.8f,
                        JudgeResult.Good => 0.5f,
                        _ => 0
                    };
                    RuntimeData.Score += (int)(multiplier * 100 * (1 + RuntimeData.Combo * 0.01f));
                    break;
            }
        }

        private void PlayHitEffect(JudgeResult result)
        {
            if (result == JudgeResult.Empty)
            {
                if (emptyHitSoundSource != null && emptyHitSoundSource.clip != null)
                    emptyHitSoundSource.Play();
            }
            else
            {
                if (hitSoundSource != null && hitSoundSource.clip != null)
                    hitSoundSource.Play();
            }

            StartCoroutine(FlashJudgeLine(result));
        }

        private IEnumerator FlashJudgeLine(JudgeResult result)
        {
            Renderer judgeRenderer = judgeLine.GetComponent<Renderer>();
            if (judgeRenderer == null) yield break;

            Material originalMat = judgeRenderer.material;
            Material flashMat = new Material(originalMat);

            flashMat.color = result switch
            {
                JudgeResult.Empty => Color.gray,
                JudgeResult.Miss => Color.red,
                _ => Color.green
            };

            judgeRenderer.material = flashMat;
            yield return new WaitForSeconds(0.1f);
            judgeRenderer.material = originalMat;
            Destroy(flashMat);
        }

        private void UpdateNotes()
        {
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                activeNotes[i].UpdatePosition();
                if (activeNotes[i].IsMissed(judgeLine.position.y))
                {
                    OnNoteJudged(activeNotes[i], JudgeResult.Miss);
                    activeNotes.RemoveAt(i);
                }
            }
        }

        public void SpawnNote(int trackIndex)
        {
            if (notePrefab == null)
            {
                UnityEngine.Debug.LogError("请赋值音符预制体（notePrefab）");
                return;
            }

            float trackX = (trackIndex - 3) * trackSpacing;
            Vector3 spawnPos = new Vector3(
                noteSpawnPoint.position.x + trackX,
                noteSpawnPoint.position.y,
                noteSpawnPoint.position.z + trackZOffset.z
            );

            GameObject noteObj = Instantiate(notePrefab, spawnPos, Quaternion.identity);
            Note note = noteObj.GetComponent<Note>();
            if (note == null) note = noteObj.AddComponent<Note>();

            note.Initialize(noteSpeed, trackIndex);
            activeNotes.Add(note);
        }

        private IEnumerator SpawnTestNotes()
        {
            yield return new WaitForSeconds(1f);
            for (int i = 0; i < 7; i++)
            {
                SpawnNote(i);
                yield return new WaitForSeconds(0.7f);
            }
        }

        private void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0 : 1;
        }

        private void RestartGame()
        {
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public class Note : MonoBehaviour
        {
            public int TrackIndex { get; private set; }
            private float moveSpeed;
            private bool isJudged = false;
            private Renderer noteRenderer;

            private void Awake()
            {
                noteRenderer = GetComponent<Renderer>();
                if (noteRenderer == null)
                    noteRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            public void Initialize(float speed, int trackIndex)
            {
                moveSpeed = speed;
                TrackIndex = trackIndex;
                if (noteRenderer != null)
                    noteRenderer.material.color = Color.white;
            }

            public void UpdatePosition()
            {
                if (isJudged) return;
                transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);
            }

            public bool IsMissed(float judgeLineY)
            {
                return !isJudged && transform.position.y < judgeLineY - 0.5f;
            }

            public void OnJudged(JudgeResult result)
            {
                isJudged = true;
                if (noteRenderer != null)
                {
                    noteRenderer.material.color = result switch
                    {
                        JudgeResult.Miss => Color.gray,
                        _ => Color.yellow
                    };
                }
                Destroy(gameObject, 0.3f);
            }
        }
    }

    public static class RuntimeData
    {
        public static int Score { get; set; } = 0;
        public static int HitCount { get; set; } = 0;
        public static int MissCount { get; set; } = 0;
        public static int Combo { get; set; } = 0;
    }
}