using DG.Tweening;
using MusicGame.SelectMusic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RuntimeDataNS = MusicGame.SelectMusic.Utils.RuntimeData; // 添加RuntimeData命名空间别名
using UnityDebug = UnityEngine.Debug;

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

    private float hitCount;
    private float missCount;
    private float maxCombo;
    private float score;
    private bool scoreAnimationDone = false;
    private int currentSelection = 0;
    private Button[] buttons;


    private void Start()
    {
        buttons = new Button[2] { retryButton, backButton };
        if (retryButton != null) retryButton.onClick.AddListener(RetryGame);
        if (backButton != null) backButton.onClick.AddListener(BackToMenu);
        UpdateButtonSelection();
    }

    private void Update()
    {
        if (!scoreAnimationDone) PlayScoreAnimation();
        else HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.A)) { currentSelection = 0; UpdateButtonSelection(); }
        if (Input.GetKeyDown(KeyCode.D)) { currentSelection = 1; UpdateButtonSelection(); }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentSelection == 0 && retryButton != null) RetryGame();
            else if (currentSelection == 1 && backButton != null) BackToMenu();
        }
    }

    private void UpdateButtonSelection()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            var text = buttons[i].GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null) text.color = (i == currentSelection) ? selectedColor : normalColor;
        }
    }

    private void PlayScoreAnimation()
    {
        if (perfectCountLabel == null || missCountLabel == null || comboCountLabel == null || scoreCountLabel == null) return;

        hitCount = Mathf.SmoothStep(hitCount, RuntimeDataNS.HitCount, 0.1f); // 替换为RuntimeDataNS
        perfectCountLabel.text = $"{Mathf.RoundToInt(hitCount)}/{RuntimeDataNS.HitCount + RuntimeDataNS.MissCount}"; // 替换为RuntimeDataNS

        missCount = Mathf.SmoothStep(missCount, RuntimeDataNS.MissCount, 0.1f); // 替换为RuntimeDataNS
        missCountLabel.text = Mathf.RoundToInt(missCount).ToString();

        maxCombo = Mathf.SmoothStep(maxCombo, RuntimeDataNS.MaxCombo, 0.1f); // 替换为RuntimeDataNS
        comboCountLabel.text = Mathf.RoundToInt(maxCombo).ToString();

        score = Mathf.SmoothStep(score, RuntimeDataNS.Score, 0.1f); // 替换为RuntimeDataNS
        scoreCountLabel.text = score.ToString("0.00");

        if (Mathf.RoundToInt(hitCount) == RuntimeDataNS.HitCount && // 替换为RuntimeDataNS
            Mathf.RoundToInt(missCount) == RuntimeDataNS.MissCount && // 替换为RuntimeDataNS
            Mathf.RoundToInt(maxCombo) == RuntimeDataNS.MaxCombo && // 替换为RuntimeDataNS
            Mathf.Abs(RuntimeDataNS.Score - score) < 0.00005f) // 替换为RuntimeDataNS
        {
            scoreAnimationDone = true;
            if (rankGroup != null)
                rankGroup.DOFade(1, 0.75f).SetEase(Ease.InOutCubic).SetDelay(0.5f);
        }
    }

    private void RetryGame() => SceneManager.LoadScene(string.IsNullOrEmpty(gameSceneName) ? "Game" : gameSceneName);
    private void BackToMenu() => SceneManager.LoadScene(string.IsNullOrEmpty(menuSceneName) ? "SelectMusic" : menuSceneName);
}