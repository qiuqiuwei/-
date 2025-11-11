using UnityEngine;
using UnityEngine.SceneManagement;
using MusicGame.SelectMusic.Utils;
using System.Collections;

public class InitManager : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(Utils.WaitAndAction(4f, () =>
        {
            Utils.FadeOut(1f, () =>
            {
                SceneManager.LoadScene("Startup");
            });
        }));
    }
}