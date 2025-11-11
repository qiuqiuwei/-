using DG.Tweening;
using System;
using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI; // 引入UI命名空间
using UIImage = UnityEngine.UI.Image; // 定义别名

namespace MusicGame.SelectMusic.Utils
{
    public static class Utils
    {
        public static string CustomAudioFolder { get; set; } = "";
        public static string CustomCoverFolder { get; set; } = "";

        public static Sprite LoadBanner(string bannerFilename, UIImage defaultImage = null)
        {
            if (string.IsNullOrEmpty(bannerFilename))
                return defaultImage?.sprite;

            var customPath = System.IO.Path.Combine(CustomCoverFolder, bannerFilename);
            var banner = Resources.Load<Sprite>(customPath);

            if (banner == null)
            {
                var defaultPath = System.IO.Path.Combine(Model.GameConst.BANNER_PATH, bannerFilename);
                banner = Resources.Load<Sprite>(defaultPath);
            }

            return banner ?? defaultImage?.sprite;
        }


        public static ResourceRequest LoadAudioAsync(string audioFilename)
        {
            if (string.IsNullOrEmpty(audioFilename))
                return null;

            var customPath = System.IO.Path.Combine(CustomAudioFolder, audioFilename);
            var request = Resources.LoadAsync<AudioClip>(customPath);

            if (request == null)
            {
                var defaultPath = System.IO.Path.Combine(Model.GameConst.AUDIO_PATH, audioFilename);
                request = Resources.LoadAsync<AudioClip>(defaultPath);
            }

            return request;
        }

        public static void FadeOut(float duration, Action onComplete)
        {
            CanvasGroup canvasGroup = GameObject.FindObjectOfType<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.LogWarning("Utils.FadeOut: 找不到CanvasGroup");
                onComplete?.Invoke();
                return;
            }

            canvasGroup.DOFade(0, duration).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                onComplete?.Invoke();
            });
        }

        public static IEnumerator WaitAndAction(float waitSeconds, Action action)
        {
            yield return new WaitForSeconds(waitSeconds);
            action?.Invoke();
        }
    }
}