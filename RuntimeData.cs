using MusicGame.SelectMusic.Model;

namespace MusicGame.SelectMusic.Utils
{
    public static class RuntimeData
    {
        public static Music selectedMusic;
        public static Beatmap selectedBeatmap;
        public static int selectedMusicIndex = 0;
        public static int selectedBeatmapIndex = 0;
        public static bool useCustomMusic = false;
    }
}
