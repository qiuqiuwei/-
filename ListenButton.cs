using UnityEngine;
using UnityEngine.UI;

public class ListenButton : MonoBehaviour
{
    private Button button;
    private bool isListening = false;
    public KeyCode triggerKey = KeyCode.L;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnListenButtonClick);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            OnListenButtonClick();
        }
    }

    void OnListenButtonClick()
    {
        isListening = !isListening;

        if (isListening)
        {
            UnityEngine.Debug.Log("ø™ º‘§¿¿“Ù¿÷");
            // AudioManager.Instance.PlayPreview();
        }
        else
        {
            UnityEngine.Debug.Log("Õ£÷π‘§¿¿“Ù¿÷");
            // AudioManager.Instance.StopPreview();
        }

        UnityEngine.UI.Text buttonText = GetComponentInChildren<UnityEngine.UI.Text>();
        if (buttonText != null)
        {
            buttonText.text = isListening ? "Õ£÷π‘§¿¿" : "‘§¿¿";
        }
    }
}