using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Model = MusicGame.SelectMusic.Model;
using RuntimeDataNS = MusicGame.SelectMusic.Utils.RuntimeData;
using GameConstNS = MusicGame.SelectMusic.Utils.GameConst; // 新增GameConst命名空间别名
using UnityDebug = UnityEngine.Debug;
using Utils = MusicGame.SelectMusic.Utils;

namespace MusicGame.SelectMusic.Manager
{
    public enum Direction { Left, Right }

    public class SelectMusicManager : MonoBehaviour
    {
        public static SelectMusicManager instance { get; private set; }
        private AudioSource audioSource;
        private Coroutine playMusicCoroutine;

        [Header("UI引用")]
        [SerializeField] private UnityEngine.UI.Image bannerBackground;
        [SerializeField] private GameObject musicUIGroup;
        [SerializeField] private GameObject musicUIItemPrefab;

        [Header("UI参数")]
        [SerializeField] private float itemWidth = 240f;
        [SerializeField] private float itemSpacing = 100f;
        [SerializeField] private float itemMaxScale = 1f;
        [SerializeField] private float itemMinScale = 0.7f;
        [SerializeField] private float swipeTransitionDuration = 0.5f;

        [Header("控制锁定时间")]
        [SerializeField] private float positiveUnlockDelay = 0.15f;
        [SerializeField] private float negativeUnlockDelay = 0.5f;

        private float backgroundAlpha;
        private Transform canvasTransform;
        private Vector3 canvasBasePosition;
        private List<Model.Music> musicList = new List<Model.Music>();
        private List<MusicUIItem> musicUIItemList = new List<MusicUIItem>();
        private int focusIndex;
        private bool lockLeftControl;
        private bool lockRightControl;
        private Coroutine unlockLeftCoroutine;
        private Coroutine unlockRightCoroutine;
        private bool changingDifficulty, isStartingGame, backingToStartup;

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            audioSource = GetComponent<AudioSource>();
            canvasTransform = GameObject.Find("Canvas")?.transform;
            if (canvasTransform != null) canvasBasePosition = canvasTransform.position;
            backgroundAlpha = bannerBackground?.color.a ?? 1f;
            focusIndex = Mathf.Clamp(RuntimeDataNS.selectedMusicIndex, 0, int.MaxValue);
        }

        void Start() { LoadMusicList(); InitMusicGroup(); }

        void Update()
        {
            if (isStartingGame || backingToStartup) return;
            CheckSwipe(); CheckChangeDifficulty(); CheckGameStart(); CheckBackToStartup();
        }

        private void LoadMusicList()
        {
            musicList.Clear();
            TextAsset[] assets = Resources.LoadAll<TextAsset>(GameConstNS.BEATMAP_PATH); // 修正GameConst引用
            if (assets == null || assets.Length == 0)
            {
                UnityDebug.LogError($"未找到谱面资源，路径：{GameConstNS.BEATMAP_PATH}");
                return;
            }

            foreach (var asset in assets)
            {
                try
                {
                    Model.Music music = Model.Music.FromJson(asset.text);
                    if (string.IsNullOrEmpty(music.title) || music.beatmapList == null || music.beatmapList.Count == 0)
                    {
                        UnityDebug.LogWarning($"谱面 {asset.name} 数据不完整，跳过加载");
                        continue;
                    }
                    musicList.Add(music);
                    UnityDebug.Log($"加载谱面成功：{music.title}");
                }
                catch (Exception e)
                {
                    UnityDebug.LogError($"解析谱面 {asset.name} 失败：{e.Message}");
                }
            }
        }

        private void InitMusicGroup()
        {
            foreach (var item in musicUIItemList)
                if (item.gameObject != null) Destroy(item.gameObject);
            musicUIItemList.Clear();

            if (musicList.Count == 0)
            {
                UnityDebug.LogError("音乐列表为空，无法初始化UI");
                return;
            }

            focusIndex = Mathf.Clamp(focusIndex, 0, musicList.Count - 1);
            RuntimeDataNS.selectedMusicIndex = focusIndex;

            for (int i = 0; i < musicList.Count; i++)
            {
                Model.Music music = musicList[i];
                MusicUIItem item = CreateMusicUIItem(music);
                if (item != null)
                {
                    musicUIItemList.Add(item);
                    item.transform.localPosition = new Vector3(i * (itemWidth + itemSpacing), 0, 0);
                }
            }

            musicUIGroup.transform.localPosition = new Vector3(-focusIndex * (itemWidth + itemSpacing), 0, 0);
            if (musicUIItemList.Count > 0)
                SetDefaultFocusItem(musicUIItemList[focusIndex]);
        }

        private MusicUIItem CreateMusicUIItem(Model.Music music)
        {
            if (musicUIItemPrefab == null)
            {
                UnityDebug.LogError("未赋值musicUIItemPrefab");
                return null;
            }

            GameObject itemGameObject = Instantiate(musicUIItemPrefab, musicUIGroup.transform);
            itemGameObject.name = $"MusicItem_{music.title}";
            itemGameObject.SetActive(true);

            MusicUIItem item = new MusicUIItem()
            {
                gameObject = itemGameObject,
                music = music,
                beatmapIndex = 0,
                transform = itemGameObject.transform
            };

            item.albumImage = item.transform.Find("AlbumBackground/AlbumImage")?.GetComponent<UnityEngine.UI.Image>();
            item.textGroup = item.transform.Find("TextGroup")?.GetComponent<CanvasGroup>();
            item.titleLabel = item.transform.Find("TextGroup/TitleBackground/TitleLabel")?.GetComponent<UnityEngine.UI.Text>();
            item.artistLabel = item.transform.Find("TextGroup/ArtistBackground/ArtistLabel")?.GetComponent<UnityEngine.UI.Text>();
            item.difficultyLabel = item.transform.Find("TextGroup/DifficultyBackground/DifficultyLabel")?.GetComponent<UnityEngine.UI.Text>();

            if (item.albumImage == null || item.titleLabel == null || item.artistLabel == null || item.difficultyLabel == null)
            {
                UnityDebug.LogError($"音乐项 {music.title} 缺少UI组件");
                Destroy(itemGameObject);
                return null;
            }

            item.transform.localScale = Vector3.one * itemMinScale;
            item.albumImage.sprite = Utils.LoadBanner(music.bannerFilename, item.albumImage);
            item.titleLabel.text = music.title;
            item.artistLabel.text = music.artist;

            if (music.beatmapList != null && music.beatmapList.Count > 0)
            {
                int safeIndex = Mathf.Clamp(0, 0, music.beatmapList.Count - 1);
                item.beatmapIndex = safeIndex;
                item.difficultyLabel.text = music.beatmapList[safeIndex].difficultyName;
                item.difficultyLabel.color = music.beatmapList[safeIndex].difficultyDisplayColor.ToColor();
            }
            else
            {
                item.difficultyLabel.text = "无难度";
                item.difficultyLabel.color = Color.gray;
            }

            if (item.textGroup != null)
                item.textGroup.alpha = 0;

            return item;
        }

        private void SetDefaultFocusItem(MusicUIItem item)
        {
            if (item == null)
            {
                UnityDebug.LogError("设置焦点项失败：项为空");
                return;
            }

            Model.Music music = item.music;
            if (music == null || music.beatmapList == null || music.beatmapList.Count == 0)
            {
                UnityDebug.LogError("设置焦点项失败：音乐数据不完整");
                return;
            }

            int safeBeatmapIndex = Mathf.Clamp(RuntimeDataNS.selectedBeatmapIndex, 0, music.beatmapList.Count - 1);
            item.beatmapIndex = safeBeatmapIndex;
            RuntimeDataNS.selectedBeatmapIndex = safeBeatmapIndex;

            item.difficultyLabel.text = music.beatmapList[safeBeatmapIndex].difficultyName;
            item.difficultyLabel.color = music.beatmapList[safeBeatmapIndex].difficultyDisplayColor.ToColor();

            item.transform.localScale = Vector3.one * itemMaxScale;
            if (item.textGroup != null)
                item.textGroup.alpha = 1;

            bannerBackground.sprite = item.albumImage.sprite;

            if (playMusicCoroutine != null)
                StopCoroutine(playMusicCoroutine);

            playMusicCoroutine = StartCoroutine(LoadAsyncAndPlay(music));
        }

        private void CheckSwipe()
        {
            if (lockLeftControl && lockRightControl)
                return;

            if (Input.GetKeyDown(KeyCode.LeftArrow) && !lockLeftControl)
                SwipeTo(Direction.Left);
            if (Input.GetKeyDown(KeyCode.RightArrow) && !lockRightControl)
                SwipeTo(Direction.Right);
        }

        private void SwipeTo(Direction direction)
        {
            int nextFocus = focusIndex + (direction == Direction.Left ? -1 : 1);

            if (nextFocus < 0 || nextFocus >= musicUIItemList.Count)
                return;

            lockLeftControl = true;
            lockRightControl = true;

            if (unlockLeftCoroutine != null)
                StopCoroutine(unlockLeftCoroutine);
            if (unlockRightCoroutine != null)
                StopCoroutine(unlockRightCoroutine);

            unlockLeftCoroutine = StartCoroutine(UnlockControlAfter(positiveUnlockDelay, Direction.Left));
            unlockRightCoroutine = StartCoroutine(UnlockControlAfter(negativeUnlockDelay, Direction.Right));

            SwitchMusic(nextFocus);
        }

        private IEnumerator UnlockControlAfter(float delay, Direction direction)
        {
            yield return new WaitForSeconds(delay);

            switch (direction)
            {
                case Direction.Left:
                    lockLeftControl = false;
                    break;
                case Direction.Right:
                    lockRightControl = false;
                    break;
            }
        }

        private void SwitchMusic(int nextFocus)
        {
            if (nextFocus < 0 || nextFocus >= musicUIItemList.Count)
            {
                UnityDebug.LogError($"切换音乐失败，索引超出范围：{nextFocus}");
                return;
            }

            MusicUIItem currentItem = musicUIItemList[focusIndex];
            MusicUIItem nextItem = musicUIItemList[nextFocus];

            if (currentItem == null || nextItem == null)
            {
                UnityDebug.LogError("切换音乐失败，音乐UI项为空");
                return;
            }

            float targetX = -nextFocus * (itemWidth + itemSpacing);

            musicUIGroup.transform.DOPause();
            musicUIGroup.transform.DOLocalMoveX(targetX, swipeTransitionDuration, true)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    bannerBackground.sprite = nextItem.albumImage.sprite;
                    bannerBackground.DOFade(backgroundAlpha, swipeTransitionDuration).SetEase(Ease.OutQuad);

                    if (playMusicCoroutine != null)
                        StopCoroutine(playMusicCoroutine);

                    playMusicCoroutine = StartCoroutine(LoadAsyncAndPlay(nextItem.music));
                });

            currentItem.transform.DOScale(itemMinScale, swipeTransitionDuration).SetEase(Ease.OutQuad);
            nextItem.transform.DOScale(itemMaxScale, swipeTransitionDuration).SetEase(Ease.OutQuad);

            if (currentItem.textGroup != null)
                currentItem.textGroup.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad);
            if (nextItem.textGroup != null)
                nextItem.textGroup.DOFade(1, swipeTransitionDuration).SetEase(Ease.OutQuad);

            audioSource.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad);
            bannerBackground.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad);

            focusIndex = nextFocus;
            RuntimeDataNS.selectedMusicIndex = focusIndex;
        }

        private IEnumerator LoadAsyncAndPlay(Model.Music music)
        {
            if (music == null || string.IsNullOrEmpty(music.audioFilename))
            {
                UnityDebug.LogError("加载音频失败：音乐信息不完整");
                yield break;
            }

            ResourceRequest request = Utils.LoadAudioAsync(music.audioFilename);
            if (request == null)
            {
                UnityDebug.LogError("加载音频失败：资源请求为空");
                yield break;
            }

            yield return request;

            audioSource.clip = request.asset as AudioClip;
            if (audioSource.clip == null)
            {
                UnityDebug.LogError($"加载音频失败：{music.audioFilename}");
                yield break;
            }

            audioSource.time = Mathf.Max(0, music.previewTime);
            audioSource.Play();
            audioSource.DOFade(1f, swipeTransitionDuration).SetEase(Ease.InQuad);
        }

        private void CheckChangeDifficulty()
        {
            if (changingDifficulty || musicUIItemList.Count == 0) return;

            MusicUIItem currentItem = musicUIItemList[focusIndex];
            Model.Music currentMusic = currentItem.music;
            if (currentMusic.beatmapList.Count <= 1) return;

            int newIndex = currentItem.beatmapIndex;
            if (Input.GetKeyDown(KeyCode.UpArrow))
                newIndex = (newIndex + 1) % currentMusic.beatmapList.Count;
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                newIndex = (newIndex - 1 + currentMusic.beatmapList.Count) % currentMusic.beatmapList.Count;
            else
                return;

            changingDifficulty = true;
            Model.Beatmap newBeatmap = currentMusic.beatmapList[newIndex];

            currentItem.difficultyLabel.DOFade(0, 0.2f).SetEase(Ease.InQuad).OnComplete(() =>
            {
                currentItem.beatmapIndex = newIndex;
                currentItem.difficultyLabel.text = newBeatmap.difficultyName;

                Color newColor = newBeatmap.difficultyDisplayColor.ToColor();
                newColor.a = 0;
                currentItem.difficultyLabel.color = newColor;

                currentItem.difficultyLabel.DOFade(1, 0.3f).SetEase(Ease.OutQuad).OnComplete(() =>
                {
                    changingDifficulty = false;
                    RuntimeDataNS.selectedBeatmapIndex = newIndex;
                });
            });
        }

        private void CheckGameStart()
        {
            if (Input.GetKeyDown(KeyCode.Space)) StartGame();
        }

        public void StartGame()
        {
            if (isStartingGame || musicList.Count == 0 || focusIndex < 0 || focusIndex >= musicList.Count) return;

            isStartingGame = true;
            Model.Music selectedMusic = musicList[focusIndex];
            if (selectedMusic == null || selectedMusic.beatmapList == null || selectedMusic.beatmapList.Count == 0)
            {
                UnityDebug.LogError("启动游戏失败：音乐数据不完整");
                isStartingGame = false;
                return;
            }

            MusicUIItem focusItem = musicUIItemList[focusIndex];
            int beatmapIndex = Mathf.Clamp(focusItem.beatmapIndex, 0, selectedMusic.beatmapList.Count - 1);

            RuntimeDataNS.selectedMusic = selectedMusic;
            RuntimeDataNS.selectedBeatmap = selectedMusic.beatmapList[beatmapIndex];
            RuntimeDataNS.selectedMusicIndex = focusIndex;
            RuntimeDataNS.selectedBeatmapIndex = beatmapIndex;

            Utils.FadeOut(1f, () => SceneManager.LoadScene("Game"));
        }

        private void CheckBackToStartup()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) BackToStartup();
        }

        private void BackToStartup()
        {
            if (backingToStartup) return;

            backingToStartup = true;
            Utils.FadeOut(1f, () => SceneManager.LoadScene("Startup"));
        }
    }

    public class MusicUIItem
    {
        public GameObject gameObject;
        public Transform transform;
        public Model.Music music;
        public int beatmapIndex;
        public UnityEngine.UI.Image albumImage;
        public CanvasGroup textGroup;
        public UnityEngine.UI.Text titleLabel;
        public UnityEngine.UI.Text artistLabel;
        public UnityEngine.UI.Text difficultyLabel;
    }
}