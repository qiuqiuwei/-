using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityDebug = UnityEngine.Debug; // 解决Debug歧义
using MusicGame.SelectMusic; // 关键修复：添加RuntimeData所在命名空间

public class GradeManager : MonoBehaviour
{
    [Header("UI文本组件")]
    [SerializeField] private UnityEngine.UI.Text comboCountLabel;
    [SerializeField] private UnityEngine.UI.Text scoreCountLabel;
    [SerializeField] private UnityEngine.UI.Text perfectCountLabel;
    [SerializeField] private UnityEngine.UI.Text missCountLabel;
    [SerializeField] private CanvasGroup rankGroup;

    [Header("按钮配置")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("场景配置")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string menuSceneName = "SelectMusic";

    // 分数动画变量
    private float hitCount;
    private float missCount;
    private float maxCombo;
    private float score;
    private bool scoreAnimationDone = false;

    // 键盘选择变量
    private int currentSelection = 0;
    private Button[] buttons;


    private void Start()
    {
        // 初始化按钮数组
        buttons = new Button[2] { retryButton, backButton };

        // 绑定按钮点击事件
        if (retryButton != null)
            retryButton.onClick.AddListener(RetryGame);
        if (backButton != null)
            backButton.onClick.AddListener(BackToMenu);

        // 初始化按钮选中状态
        UpdateButtonSelection();
    }

    private void Update()
    {
        // 先播放分数动画，完成后允许键盘操作
        if (!scoreAnimationDone)
        {
            PlayScoreAnimation();
        }
        else
        {
            HandleKeyboardInput();
        }
    }

    // 键盘控制逻辑：A/D切换选择，空格确定
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            currentSelection = 0; // 选中Retry
            UpdateButtonSelection();
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            currentSelection = 1; // 选中Back
            UpdateButtonSelection();
        }

        // 空格键触发选中按钮
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentSelection == 0 && retryButton != null)
                RetryGame();
            else if (currentSelection == 1 && backButton != null)
                BackToMenu();
        }
    }

    // 更新按钮选中状态（颜色反馈）
    private void UpdateButtonSelection()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue; // 跳过未赋值按钮

            // 获取按钮文本组件
            UnityEngine.UI.Text buttonText = buttons[i].GetComponentInChildren<UnityEngine.UI.Text>();
            if (buttonText != null)
            {
                buttonText.color = (i == currentSelection) ? selectedColor : normalColor;
            }
        }
    }

    // 分数平滑动画逻辑
    private void PlayScoreAnimation()
    {
        // 空引用保护
        if (perfectCountLabel == null || missCountLabel == null ||
            comboCountLabel == null || scoreCountLabel == null)
            return;

        // 关键修复：RuntimeData来自MusicGame.SelectMusic命名空间
        hitCount = Mathf.SmoothStep(hitCount, RuntimeData.HitCount, 0.1f);
        perfectCountLabel.text = $"{Mathf.RoundToInt(hitCount)}/{RuntimeData.HitCount + RuntimeData.MissCount}";

        missCount = Mathf.SmoothStep(missCount, RuntimeData.MissCount, 0.1f);
        missCountLabel.text = Mathf.RoundToInt(missCount).ToString();

        maxCombo = Mathf.SmoothStep(maxCombo, RuntimeData.MaxCombo, 0.1f);
        comboCountLabel.text = Mathf.RoundToInt(maxCombo).ToString();

        score = Mathf.SmoothStep(score, RuntimeData.Score, 0.1f);
        scoreCountLabel.text = score.ToString("0.00");

        // 判定动画是否完成
        if (Mathf.RoundToInt(hitCount) == RuntimeData.HitCount &&
            Mathf.RoundToInt(missCount) == RuntimeData.MissCount &&
            Mathf.RoundToInt(maxCombo) == RuntimeData.MaxCombo &&
            Mathf.Abs(RuntimeData.Score - score) < 0.00005f)
        {
            scoreAnimationDone = true;
            // 排名淡入动画
            if (rankGroup != null)
                rankGroup.DOFade(1, 0.75f).SetEase(Ease.InOutCubic).SetDelay(0.5f);
        }
    }

    // 重试游戏
    private void RetryGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            UnityDebug.LogError("请在Inspector设置游戏场景名称（gameSceneName）");
    }

    // 返回菜单
    private void BackToMenu()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
        else
            UnityDebug.LogError("请在Inspector设置菜单场景名称（menuSceneName）");
    }
}