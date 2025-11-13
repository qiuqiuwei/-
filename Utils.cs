using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UIImage = UnityEngine.UI.Image;
using GameConstNS = MusicGame.SelectMusic.Utils.GameConst; // 修正GameConst命名空间别名
using UnityDebug = UnityEngine.Debug; // 解决Debug歧义

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
                var defaultPath = System.IO.Path.Combine(GameConstNS.BANNER_PATH, bannerFilename); // 引用正确的GameConst命名空间
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
                var defaultPath = System.IO.Path.Combine(GameConstNS.AUDIO_PATH, audioFilename); // 引用正确的GameConst命名空间
                request = Resources.LoadAsync<AudioClip>(defaultPath);
            }

            return request;
        }

        public static void FadeOut(float duration, Action onComplete)
        {
            CanvasGroup canvasGroup = GameObject.FindObjectOfType<CanvasGroup>();
            if (canvasGroup == null)
            {
                UnityDebug.LogWarning("Utils.FadeOut: 找不到CanvasGroup"); // 替换为UnityDebug避免歧义
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