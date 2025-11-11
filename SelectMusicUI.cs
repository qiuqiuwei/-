using UnityEngine;
using UnityEngine.UI;
using Model = MusicGame.SelectMusic.Model;
using UIImage = UnityEngine.UI.Image;
using UIText = UnityEngine.UI.Text;

namespace MusicGame.SelectMusic
{
    public class SelectMusicUI : MonoBehaviour
    {
        private Model.Music currentMusic;

        // 这里可以写方法，逻辑等
    }

    public class MusicUIItem
    {
        public GameObject gameObject;
        public Transform transform;
        public Model.Music music;
        public int beatmapIndex;
        public UIImage albumImage;
        public CanvasGroup textGroup;
        public UIText titleLabel;
        public UIText artistLabel;
        public UIText difficultyLabel;
    }
}
