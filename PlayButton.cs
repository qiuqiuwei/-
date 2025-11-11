using UnityEngine;
using UnityEngine.SceneManagement; // 确保包含这个命名空间
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    private Button button;
    // 可在Inspector面板自定义触发按键（默认设置为空格键）
    public KeyCode triggerKey = KeyCode.Space;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnPlayButtonClick);
        }
    }

    void Update()
    {
        // 检测指定键盘按键按下，触发与点击相同的逻辑
        if (Input.GetKeyDown(triggerKey))
        {
            OnPlayButtonClick();
        }
    }

    void OnPlayButtonClick()
    {
        // 明确使用UnityEngine命名空间下的Debug，避免歧义
        UnityEngine.Debug.Log("Play button triggered (点击或键盘操作)!");

        // 加载游戏场景
        SceneManager.LoadScene("Game"); // 确保此处是你要加载的场景的名称
    }
}