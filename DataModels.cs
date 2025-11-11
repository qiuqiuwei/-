using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace MusicGame.Core
{
    [Serializable]
    public class Music
    {
        public string title;
        public string artist;
        public string audioFilename;
        public float previewTime;
        public string soundEffectFilename;
        public string bannerFilename;
        public List<Beatmap> beatmapList = new List<Beatmap>();

        public static Music FromJson(string json) => JsonConvert.DeserializeObject<Music>(json);
        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    [Serializable]
    public class Beatmap
    {
        public string creator;
        public string version;
        public int difficulty;
        public string difficultyName;
        public SimpleColor difficultyDisplayColor;
        public List<Note> noteList = new List<Note>();
    }

    public enum NoteType
    {
        Circle,   // 基础点击
        Slider,   // 滑动
        Spinner,  // 旋转
        Hit,      // 打击
        Slide     // 滑动（兼容旧类型）
    }

    [Serializable]
    public class Note
    {
        public NoteType type;
        public float time;
        public float speed;
        public SimpleColor color;
        public int x;
        public int y;
        public float length;      // 用于Slider
        public int repeat;        // 用于Slider
        public float endTime;     // 用于Spinner
    }

    [Serializable]
    public class SimpleColor
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public SimpleColor() { }

        public SimpleColor(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }

        public Color ToColor() => new Color(r, g, b, a);
    }
}