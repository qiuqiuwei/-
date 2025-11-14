using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Model = MusicGame.SelectMusic.Model;

namespace MusicGame.SelectMusic
{
    // 单一来源的 UI 数据结构，供 SelectMusicManager / UI 共享
    public class MusicUIItem
    {
        public GameObject gameObject;
        public Transform transform;
        public Model.Music music;
        public int beatmapIndex;
        public Image albumImage;
        public CanvasGroup textGroup;
        public Text titleLabel;
        public Text artistLabel;
        public Text difficultyLabel;
    }
}