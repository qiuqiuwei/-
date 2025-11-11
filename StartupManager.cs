using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MusicGame.SelectMusic; // 修正命名空间引用，精确到Utils所在的子命名空间

public class StartupManager : MonoBehaviour
{
    [SerializeField] private RectTransform _customOption;
    [SerializeField] private RectTransform _selectOption;

    private enum Choices { None, Custom, BuildIn }
    private enum Direction { Left, Right }

    private Choices _currentChoice = Choices.None;
    private bool _isChoosing = false;
    private bool _hasMadeChoice = false;

    private const float SELECTED_SCALE = 0.6f;
    private const float UNSELECTED_SCALE = 0.5f;
    private const float CONFIRM_SCALE = 0.7f;
    private const float ANIMATION_DURATION = 0.3f;

    private void Start()
    {
        FindUIElements();

        if (_customOption == null || _selectOption == null)
        {
            UnityEngine.Debug.LogError("UI选项未在Inspector中分配，且无法自动找到!");
            enabled = false;
            return;
        }

        SetOptionScale(_customOption, UNSELECTED_SCALE);
        SetOptionScale(_selectOption, UNSELECTED_SCALE);
    }

    private void FindUIElements()
    {
        if (_customOption == null)
        {
            GameObject customObj = GameObject.Find("CustomOption");
            if (customObj != null)
            {
                _customOption = customObj.GetComponent<RectTransform>();
                UnityEngine.Debug.Log("已自动找到 CustomOption: " + (_customOption != null));
            }
        }

        if (_selectOption == null)
        {
            GameObject selectObj = GameObject.Find("SelectOption");
            if (selectObj != null)
            {
                _selectOption = selectObj.GetComponent<RectTransform>();
                UnityEngine.Debug.Log("已自动找到 SelectOption: " + (_selectOption != null));
            }
        }
    }

    private void Update()
    {
        if (!_hasMadeChoice)
        {
            CheckInput();
        }
    }

    private void CheckInput()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SelectOption(Direction.Left);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            SelectOption(Direction.Right);
        }

        if (Input.GetKeyDown(KeyCode.Space) && _currentChoice != Choices.None)
        {
            ConfirmChoice();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
        }
    }

    private void SelectOption(Direction direction)
    {
        if (_isChoosing) return;

        if (_customOption == null || _selectOption == null)
        {
            UnityEngine.Debug.LogError("无法选择选项 - UI元素为空!");
            return;
        }

        _currentChoice = direction == Direction.Left ? Choices.Custom : Choices.BuildIn;

        if (direction == Direction.Left)
        {
            AnimateOptionScale(_customOption, SELECTED_SCALE);
            AnimateOptionScale(_selectOption, UNSELECTED_SCALE);
        }
        else
        {
            AnimateOptionScale(_selectOption, SELECTED_SCALE);
            AnimateOptionScale(_customOption, UNSELECTED_SCALE);
        }
    }

    private void ConfirmChoice()
    {
        _isChoosing = true;

        if (_customOption == null || _selectOption == null)
        {
            UnityEngine.Debug.LogError("无法确认选择 - UI元素为空!");
            return;
        }

        switch (_currentChoice)
        {
            case Choices.BuildIn:
                AnimateOptionScale(_selectOption, CONFIRM_SCALE, 2f);
                TransitionToScene("SelectMusic");
                break;

            case Choices.Custom:
                AnimateOptionScale(_customOption, CONFIRM_SCALE, 2f);
                TransitionToScene("CustomMusic");
                break;
        }
    }

    private void TransitionToScene(string sceneName)
    {
        transform.DOScale(0, 1f).OnComplete(() =>
        {
            // 此时Utils已可正常访问
            Utils.FadeOut(1f, () => SceneManager.LoadScene(sceneName));
        });
    }

    private void ExitGame()
    {
        _hasMadeChoice = true;
        // 此时Utils已可正常访问
        Utils.FadeOut(1f, () => Application.Quit());
    }

    private void AnimateOptionScale(RectTransform option, float targetScale, float duration = ANIMATION_DURATION)
    {
        if (option == null)
        {
            UnityEngine.Debug.LogError("AnimateOptionScale: option参数为空!");
            return;
        }

        option.DOScale(targetScale, duration).SetEase(Ease.OutQuad);
    }

    private void SetOptionScale(RectTransform option, float scale)
    {
        if (option == null)
        {
            UnityEngine.Debug.LogError("SetOptionScale: option参数为空!");
            return;
        }

        option.localScale = Vector3.one * scale;
    }

    public void DeactiveChoosing()
    {
        if (_hasMadeChoice) return;

        _isChoosing = false;
        transform.DOPause();
        transform.localScale = Vector3.one;

        if (_customOption == null || _selectOption == null)
        {
            UnityEngine.Debug.LogError("无法取消选择 - UI元素为空!");
            return;
        }

        if (_currentChoice == Choices.BuildIn)
        {
            AnimateOptionScale(_selectOption, SELECTED_SCALE);
        }
        else if (_currentChoice == Choices.Custom)
        {
            AnimateOptionScale(_customOption, SELECTED_SCALE);
        }
    }
}