using System;
using System.Collections.Generic;
using UnityEngine;

namespace MusicGame.SelectMusic.Model
{
    public enum NoteType
    {
        Circle,
        Slider,
        Spinner,
        Unknown
    }

    [Serializable]
    public class SimpleColor
    {
        public float r, g, b, a;

        public SimpleColor(Color color)
        {
            r = color.r; g = color.g; b = color.b; a = color.a;
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }

    [Serializable]
    public class Note
    {
        public int x;
        public int y;
        public float time;
        public NoteType type;
        public float length;
        public int repeat;
        public float endTime;
    }

    [Serializable]
    public class Beatmap
    {
        public string difficultyName;
        public SimpleColor difficultyDisplayColor;
        public string creator;
        public string version;
        public List<Note> noteList = new List<Note>();
    }

    [Serializable]
    public class Music
    {
        public string title;
        public string artist;
        public string audioFilename;
        public string bannerFilename;
        public string soundEffectFilename;
        public float previewTime;
        public List<Beatmap> beatmapList = new List<Beatmap>();

        public static Music FromJson(string json)
        {
            return JsonUtility.FromJson<Music>(json);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }
    }
}
