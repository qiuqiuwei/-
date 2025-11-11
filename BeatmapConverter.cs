using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

namespace MusicGame.SelectMusic
{
    // 类型别名定义（避免与系统类冲突）
    using UIImage = UnityEngine.UI.Image;
    using UIText = UnityEngine.UI.Text;
    using UnityDebug = UnityEngine.Debug;

    // 游戏常量配置
    public static class GameConst
    {
        public static Color RANK_A_COLOR = new Color(0.2f, 0.8f, 0.2f);
        public static Color RANK_B_COLOR = new Color(0.2f, 0.2f, 0.8f);
        public static Color RANK_D_COLOR = new Color(0.8f, 0.2f, 0.2f);

        // 资源路径配置
        public const string BEATMAP_PATH = "Music/Beatmaps";
        public const string DEFAULT_AUDIO_FOLDER = "Music/Audio";
        public const string DEFAULT_COVER_FOLDER = "Music/Banner";
        public const string SOUND_EFFECT_PATH = "Music/SoundEffect/";
    }

    // 音乐UI项数据结构
    public class MusicUIItem
    {
        public GameObject gameObject;
        public Transform transform;
        public Music music;
        public int beatmapIndex;
        public UIImage albumImage;
        public CanvasGroup textGroup;
        public UIText titleLabel;
        public UIText artistLabel;
        public UIText difficultyLabel;
    }

    // 工具类
    public static class Utils
    {
        // 路径配置（支持自定义）
        public static string CustomAudioFolder = GameConst.DEFAULT_AUDIO_FOLDER;
        public static string CustomCoverFolder = GameConst.DEFAULT_COVER_FOLDER;

        // 加载音频
        public static AudioClip LoadAudio(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                UnityDebug.LogError("[Utils] 音频文件名为空");
                return null;
            }

            string fileNameWithoutExt = RemoveExtensionName(filename);
            string fullPath = Path.Combine(CustomAudioFolder, fileNameWithoutExt).Replace("\\", "/");

            var clip = Resources.Load<AudioClip>(fullPath);
            if (clip == null)
                UnityDebug.LogError($"❌ 音频加载失败: {fullPath}\n请检查路径和文件格式");
            else
                UnityDebug.Log($"✅ 音频加载成功: {fullPath}");
            return clip;
        }

        // 异步加载音频
        public static ResourceRequest LoadAudioAsync(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                UnityDebug.LogError("[Utils] 音频文件名为空");
                return null;
            }

            string fileNameWithoutExt = RemoveExtensionName(filename);
            string fullPath = Path.Combine(CustomAudioFolder, fileNameWithoutExt).Replace("\\", "/");

            if (Resources.Load<AudioClip>(fullPath) == null)
            {
                UnityDebug.LogError($"❌ 音频资源不存在: {fullPath}");
                return null;
            }
            return Resources.LoadAsync<AudioClip>(fullPath);
        }

        // 加载封面
        public static Sprite LoadBanner(string filename, UIImage targetImage = null)
        {
            if (string.IsNullOrEmpty(filename))
            {
                UnityDebug.LogError("[Utils] 封面文件名为空");
                return null;
            }

            string fileNameWithoutExt = RemoveExtensionName(filename);
            string fullPath = Path.Combine(CustomCoverFolder, fileNameWithoutExt).Replace("\\", "/");
            var banner = Resources.Load<Sprite>(fullPath);

            if (banner == null)
            {
                UnityDebug.LogError($"❌ 封面加载失败: {fullPath}\n请检查路径和Sprite格式");
                if (targetImage != null)
                    targetImage.color = new Color(0.5f, 0.5f, 0.5f);
                return null;
            }

            if (targetImage != null)
                targetImage.color = Color.white;

            UnityDebug.Log($"✅ 封面加载成功: {fullPath}");
            return banner;
        }

        // 移除文件后缀
        public static string RemoveExtensionName(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            var dotIndex = path.LastIndexOf('.');
            if (dotIndex > 0)
            {
                var ext = path.Substring(dotIndex).ToLower();
                if (new List<string> { ".mp3", ".wav", ".ogg", ".jpg", ".png" }.Contains(ext))
                    return path.Substring(0, dotIndex);
            }
            return path;
        }

        // 场景淡入淡出
        public static void FadeOut(float duration, TweenCallback onComplete = null, float delay = 0)
        {
            var fadePanel = GetFadePanel();
            if (fadePanel == null) return;

            fadePanel.SetActive(true);
            var mask = fadePanel.transform.Find("Mask")?.GetComponent<UIImage>();
            if (mask == null)
            {
                UnityDebug.LogError("[Utils] 未找到Mask组件");
                return;
            }

            mask.color = new Color(mask.color.r, mask.color.g, mask.color.b, 0);
            mask.DOFade(1, duration)
                .SetDelay(delay)
                .SetEase(Ease.Linear)
                .OnComplete(() => onComplete?.Invoke());
        }

        private static GameObject GetFadePanel()
        {
            var uiCanvas = GameObject.Find("UICanvas");
            if (uiCanvas == null)
            {
                UnityDebug.LogError("[Utils] 未找到UICanvas");
                return null;
            }

            var fadePanel = uiCanvas.transform.Find("FadePanel");
            return fadePanel?.gameObject;
        }
    }

    // 方向枚举
    enum Direction
    {
        Left, Right
    }

    // 核心选曲管理器
    public class SelectMusicManager : MonoBehaviour
    {
        public static SelectMusicManager instance { get; private set; }

        [Header("UI引用")]
        public UIImage bannerBackground;
        public GameObject musicUIGroup;
        public GameObject musicUIItemPrefab;

        [Header("配置参数")]
        public float itemWidth = 240f;
        public float itemSpacing = 100f;
        public float itemMaxScale = 1f;
        public float itemMinScale = 0.7f;
        public float swipeTransitionDuration = 0.5f;
        public float minSwipeSpeed = 1f;

        [Header("资源路径配置")]
        public string customAudioFolder = GameConst.DEFAULT_AUDIO_FOLDER;
        public string customCoverFolder = GameConst.DEFAULT_COVER_FOLDER;

        private AudioSource audioSource;
        private Coroutine playMusicCoroutine;
        private Transform canvasTransform;
        private Vector3 canvasBasePosition;
        private List<Music> musicList;
        private List<MusicUIItem> musicUIItemList;
        private int focusIndex;
        private float backgroundAlpha;
        private bool lockLeftControl;
        private bool lockRightControl;
        private float positiveUnlockDelay = 0.15f;
        private float negativeUnlockDelay = 0.5f;
        private Coroutine unlockLeftCoroutine;
        private Coroutine unlockRightCoroutine;
        private bool changingDifficulty;
        private bool isStartingGame;
        private bool backingToStartup;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            canvasTransform = GameObject.Find("Canvas")?.transform;
            canvasBasePosition = canvasTransform?.position ?? Vector3.zero;

            musicList = new List<Music>();
            musicUIItemList = new List<MusicUIItem>();
            focusIndex = Mathf.Clamp(RuntimeData.selectedMusicIndex, 0, int.MaxValue);

            lockRightControl = false;
            lockLeftControl = false;
            backgroundAlpha = bannerBackground?.color.a ?? 1f;

            // 初始化路径配置
            Utils.CustomAudioFolder = customAudioFolder;
            Utils.CustomCoverFolder = customCoverFolder;
        }

        void Start()
        {
            UnityDebug.Log($"[加载路径] 音频: {Utils.CustomAudioFolder}, 封面: {Utils.CustomCoverFolder}");
            LoadMusicList();
            InitMusicGroup();
        }

        void LoadMusicList()
        {
            musicList.Clear();
            var assets = Resources.LoadAll<TextAsset>(GameConst.BEATMAP_PATH);

            if (assets == null || assets.Length == 0)
            {
                UnityDebug.LogError($"未找到谱面资源！路径: {GameConst.BEATMAP_PATH}");
                return;
            }

            foreach (var asset in assets)
            {
                try
                {
                    var music = Music.FromJson(asset.text);

                    UnityDebug.Log($"加载音乐: {music.title}, 音频文件: {music.audioFilename}, 封面: {music.bannerFilename}");

                    // 校验关键字段
                    if (string.IsNullOrEmpty(music.title) || string.IsNullOrEmpty(music.audioFilename))
                    {
                        UnityDebug.LogWarning($"跳过无效音乐: {asset.name}（标题或音频文件为空）");
                        continue;
                    }

                    // 新增：校验 beatmapList 是否有效
                    if (music.beatmapList == null)
                    {
                        UnityDebug.LogError($"❌ 音乐 {music.title}：beatmapList 为 null（未定义）");
                        continue; // 跳过无效音乐
                    }
                    if (music.beatmapList.Count == 0)
                    {
                        UnityDebug.LogError($"❌ 音乐 {music.title}：beatmapList 为空数组（无难度谱面）");
                        continue;
                    }

                    // 新增：校验每个 Beatmap 的 noteList 是否有效
                    bool hasValidBeatmap = false;
                    for (int j = 0; j < music.beatmapList.Count; j++)
                    {
                        var beatmap = music.beatmapList[j];
                        if (beatmap == null)
                        {
                            UnityDebug.LogError($"❌ 音乐 {music.title}：beatmapList[{j}] 为 null（单个谱面无效）");
                            continue;
                        }
                        if (beatmap.noteList == null || beatmap.noteList.Count == 0)
                        {
                            UnityDebug.LogError($"❌ 音乐 {music.title} - 难度 {beatmap.difficultyName}：noteList 为空（无音符数据）");
                        }
                        else
                        {
                            UnityDebug.Log($"✅ 音乐 {music.title} - 难度 {beatmap.difficultyName}：有效音符数 {beatmap.noteList.Count}");
                            hasValidBeatmap = true; // 标记存在有效谱面
                        }
                    }

                    // 若所有谱面都无效，则跳过该音乐
                    if (!hasValidBeatmap)
                    {
                        UnityDebug.LogError($"❌ 音乐 {music.title}：所有谱面均无有效音符数据，已跳过");
                        continue;
                    }

                    musicList.Add(music);
                    UnityDebug.Log($"加载成功: {music.title}");
                }
                catch (Exception e)
                {
                    UnityDebug.LogError($"解析失败 {asset.name}: {e.Message}");
                }
            }
        }

        void Update()
        {
            if (isStartingGame || backingToStartup) return;

            CheckSwipe();
            CheckChangeDifficulty();
            CheckGameStart();
            CheckBackToStartup();
        }

        void InitMusicGroup()
        {
            foreach (var item in musicUIItemList)
                if (item.gameObject != null) Destroy(item.gameObject);
            musicUIItemList.Clear();

            if (musicList.Count == 0)
            {
                UnityDebug.LogError("音乐列表为空！请检查谱面转换是否成功");
                return;
            }

            focusIndex = Mathf.Clamp(focusIndex, 0, musicList.Count - 1);
            RuntimeData.selectedMusicIndex = focusIndex;

            for (int i = 0; i < musicList.Count; i++)
            {
                var item = CreateMusicUIItem(musicList[i]);
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

        MusicUIItem CreateMusicUIItem(Music music)
        {
            if (musicUIItemPrefab == null)
            {
                UnityDebug.LogError("未赋值musicUIItemPrefab");
                return null;
            }

            var itemGameObject = Instantiate(musicUIItemPrefab, musicUIGroup.transform);
            itemGameObject.name = $"MusicItem_{music.title}";
            itemGameObject.SetActive(true);

            var item = new MusicUIItem
            {
                gameObject = itemGameObject,
                music = music,
                beatmapIndex = 0,
                transform = itemGameObject.transform
            };

            // 查找UI组件
            item.albumImage = item.transform.Find("AlbumBackground/AlbumImage")?.GetComponent<UIImage>();
            item.textGroup = item.transform.Find("TextGroup")?.GetComponent<CanvasGroup>();
            item.titleLabel = item.transform.Find("TextGroup/TitleBackground/TitleLabel")?.GetComponent<UIText>();
            item.artistLabel = item.transform.Find("TextGroup/ArtistBackground/ArtistLabel")?.GetComponent<UIText>();
            item.difficultyLabel = item.transform.Find("TextGroup/DifficultyBackground/DifficultyLabel")?.GetComponent<UIText>();

            // 组件校验
            if (item.albumImage == null || item.titleLabel == null || item.artistLabel == null || item.difficultyLabel == null)
            {
                UnityDebug.LogError($"UI项缺少组件: {music.title}");
                Destroy(itemGameObject);
                return null;
            }

            // 初始化UI
            item.transform.localScale = Vector3.one * itemMinScale;
            item.albumImage.sprite = Utils.LoadBanner(music.bannerFilename, item.albumImage);
            item.titleLabel.text = music.title;
            item.artistLabel.text = music.artist;

            // 初始化难度信息
            if (music.beatmapList != null && music.beatmapList.Count > 0)
            {
                var safeIndex = Mathf.Clamp(0, 0, music.beatmapList.Count - 1);
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

        void SetDefaultFocusItem(MusicUIItem item)
        {
            if (item == null)
            {
                UnityDebug.LogError("焦点项为空");
                return;
            }

            var music = item.music;
            if (music == null)
            {
                UnityDebug.LogError("音乐对象为空");
                return;
            }
            if (music.beatmapList == null || music.beatmapList.Count == 0)
            {
                UnityDebug.LogError($"音乐 {music.title} 的 beatmapList 为空！");
                return;
            }

            // 新增：输出选中音乐的beatmapList数量
            UnityDebug.Log($"📌 选中音乐 {music.title}：beatmapList 共 {music.beatmapList.Count} 个难度");

            foreach (var beatmap in music.beatmapList)
            {
                if (beatmap.noteList == null || beatmap.noteList.Count == 0)
                {
                    UnityDebug.LogError($"音乐 {music.title} 的谱面 {beatmap.difficultyName} 中 noteList 为空！");
                    return;
                }
            }

            // 同步运行时数据
            var safeBeatmapIndex = Mathf.Clamp(RuntimeData.selectedBeatmapIndex, 0, music.beatmapList.Count - 1);
            item.beatmapIndex = safeBeatmapIndex;
            RuntimeData.selectedBeatmapIndex = safeBeatmapIndex;

            // 更新UI
            item.difficultyLabel.text = music.beatmapList[safeBeatmapIndex].difficultyName;
            item.difficultyLabel.color = music.beatmapList[safeBeatmapIndex].difficultyDisplayColor.ToColor();
            item.transform.localScale = Vector3.one * itemMaxScale;
            if (item.textGroup != null)
                item.textGroup.alpha = 1;

            // 更新背景封面
            bannerBackground.sprite = item.albumImage.sprite;

            // 播放预览
            if (playMusicCoroutine != null)
                StopCoroutine(playMusicCoroutine);
            playMusicCoroutine = StartCoroutine(LoadAsyncAndPlay(music));
        }

        void CheckSwipe()
        {
            if (lockLeftControl && lockRightControl) return;

            if (Input.GetKeyDown(KeyCode.LeftArrow) && !lockLeftControl)
                SwipeTo(Direction.Left);
            if (Input.GetKeyDown(KeyCode.RightArrow) && !lockRightControl)
                SwipeTo(Direction.Right);
        }

        void SwipeTo(Direction direction)
        {
            var nextFocus = focusIndex + ((direction == Direction.Left) ? -1 : 1);
            if (nextFocus < 0 || nextFocus >= musicUIItemList.Count)
                return;

            lockLeftControl = true;
            lockRightControl = true;

            if (unlockLeftCoroutine != null) StopCoroutine(unlockLeftCoroutine);
            if (unlockRightCoroutine != null) StopCoroutine(unlockRightCoroutine);

            unlockLeftCoroutine = StartCoroutine(UnlockControlAfter(positiveUnlockDelay, Direction.Left));
            unlockRightCoroutine = StartCoroutine(UnlockControlAfter(negativeUnlockDelay, Direction.Right));

            SwitchMusic(nextFocus);
        }

        IEnumerator UnlockControlAfter(float time, Direction direction)
        {
            yield return new WaitForSeconds(time);
            switch (direction)
            {
                case Direction.Left: lockLeftControl = false; break;
                case Direction.Right: lockRightControl = false; break;
            }
        }

        void SwitchMusic(int nextFocus)
        {
            if (nextFocus < 0 || nextFocus >= musicUIItemList.Count)
            {
                UnityDebug.LogError($"索引超出范围: {nextFocus}");
                return;
            }

            var currentItem = musicUIItemList[focusIndex];
            var nextItem = musicUIItemList[nextFocus];
            if (currentItem == null || nextItem == null)
            {
                UnityDebug.LogError("当前项或下一项为空");
                return;
            }

            // 计算目标位置
            var targetX = -nextFocus * (itemWidth + itemSpacing);
            musicUIGroup.transform.DOPause();
            musicUIGroup.transform.DOLocalMoveX(targetX, swipeTransitionDuration, true)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // 切换背景封面
                    bannerBackground.sprite = nextItem.albumImage.sprite;
                    bannerBackground.DOFade(backgroundAlpha, swipeTransitionDuration).SetEase(Ease.OutQuad);

                    // 播放新音乐预览
                    if (playMusicCoroutine != null) StopCoroutine(playMusicCoroutine);
                    playMusicCoroutine = StartCoroutine(LoadAsyncAndPlay(nextItem.music));
                });

            // 动画效果
            currentItem.transform.DOScale(itemMinScale, swipeTransitionDuration).SetEase(Ease.OutQuad);
            nextItem.transform.DOScale(itemMaxScale, swipeTransitionDuration).SetEase(Ease.OutQuad);

            currentItem.textGroup?.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad);
            nextItem.textGroup?.DOFade(1, swipeTransitionDuration).SetEase(Ease.OutQuad);

            audioSource.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad);
            bannerBackground.DOFade(0, swipeTransitionDuration).SetEase(Ease.OutQuad);

            // 更新焦点索引
            focusIndex = nextFocus;
            RuntimeData.selectedMusicIndex = focusIndex;
        }

        // 异步加载并播放音乐
        IEnumerator LoadAsyncAndPlay(Music music)
        {
            if (music == null || string.IsNullOrEmpty(music.audioFilename))
            {
                UnityDebug.LogError("音乐数据或音频路径为空");
                yield break;
            }

            var request = Utils.LoadAudioAsync(music.audioFilename);
            if (request == null) yield break;

            yield return request;

            audioSource.clip = request.asset as AudioClip;
            if (audioSource.clip == null)
            {
                UnityDebug.LogError($"音频加载失败: {music.audioFilename}");
                yield break;
            }

            // 从预览时间点播放
            audioSource.time = Mathf.Max(0, music.previewTime);
            audioSource.Play();
            audioSource.DOFade(1, swipeTransitionDuration).SetEase(Ease.InQuad);
        }

        // 检查难度切换输入
        void CheckChangeDifficulty()
        {
            var direction = 0;
            if (Input.GetKeyDown(KeyCode.UpArrow)) direction = 1;
            else if (Input.GetKeyDown(KeyCode.DownArrow)) direction = -1;

            if (direction != 0) ChangeDifficulty(direction);
        }

        // 切换难度
        public void ChangeDifficulty(int direction)
        {
            if (changingDifficulty || isStartingGame) return;
            if (musicUIItemList.Count == 0 || focusIndex < 0 || focusIndex >= musicUIItemList.Count) return;

            var item = musicUIItemList[focusIndex];
            if (item == null || item.music?.beatmapList == null || item.music.beatmapList.Count <= 1) return;

            // 计算新难度索引
            var newIndex = item.beatmapIndex + direction;
            newIndex = (newIndex + item.music.beatmapList.Count) % item.music.beatmapList.Count;

            changingDifficulty = true;
            var newBeatmap = item.music.beatmapList[newIndex];

            // 难度切换动画
            item.difficultyLabel.DOFade(0, 0.3f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                item.beatmapIndex = newIndex;
                item.difficultyLabel.text = newBeatmap.difficultyName;

                var newColor = newBeatmap.difficultyDisplayColor.ToColor();
                newColor.a = 0;
                item.difficultyLabel.color = newColor;

                item.difficultyLabel.DOFade(1, 0.3f).SetEase(Ease.OutQuad).OnComplete(() =>
                {
                    changingDifficulty = false;
                    RuntimeData.selectedBeatmapIndex = newIndex; // 同步运行时数据
                });
            });
        }

        // 检查游戏启动输入
        void CheckGameStart()
        {
            if (Input.GetKeyDown(KeyCode.Space)) StartGame();
        }

        // 启动游戏
        public void StartGame()
        {
            if (isStartingGame || musicList.Count == 0 || focusIndex < 0 || focusIndex >= musicList.Count) return;

            isStartingGame = true;
            var selectedMusic = musicList[focusIndex];
            if (selectedMusic == null || selectedMusic.beatmapList == null || selectedMusic.beatmapList.Count == 0)
            {
                UnityDebug.LogError("选中的音乐数据不完整");
                isStartingGame = false;
                return;
            }

            // 同步选中数据到运行时
            var focusItem = musicUIItemList[focusIndex];
            var beatmapIndex = Mathf.Clamp(focusItem.beatmapIndex, 0, selectedMusic.beatmapList.Count - 1);

            RuntimeData.selectedMusic = selectedMusic;
            RuntimeData.selectedBeatmap = selectedMusic.beatmapList[beatmapIndex];
            RuntimeData.selectedMusicIndex = focusIndex;
            RuntimeData.selectedBeatmapIndex = beatmapIndex;
            RuntimeData.useCustomMusic = false; // 标记为非自定义音乐

            // 切换到游戏场景
            Utils.FadeOut(1f, () => SceneManager.LoadScene("Game"));
        }

        // 激活游戏启动动画
        public void ActiveGameStart()
        {
            if (isStartingGame) return;

            isStartingGame = true;
            transform.DOScale(0, 1f).OnComplete(StartGame);

            if (musicUIItemList.Count > 0 && focusIndex < musicUIItemList.Count)
                musicUIItemList[focusIndex].transform.DOScale(1.2f, 2f).SetEase(Ease.Linear);
        }

        // 取消游戏启动
        public void DeactiveGameStart()
        {
            if (!isStartingGame) return;

            isStartingGame = false;
            transform.DOPause();
            transform.localScale = Vector3.one;

            if (musicUIItemList.Count > 0 && focusIndex < musicUIItemList.Count)
            {
                musicUIItemList[focusIndex].transform.DOPause();
                musicUIItemList[focusIndex].transform.DOScale(itemMaxScale, 0.3f).SetEase(Ease.OutQuad);
            }
        }

        // 检查返回主菜单输入
        void CheckBackToStartup()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) BackToStartup();
        }

        // 返回主菜单
        void BackToStartup()
        {
            if (backingToStartup) return;

            backingToStartup = true;
            Utils.FadeOut(1f, () => SceneManager.LoadScene("Startup"));
        }
    }
}