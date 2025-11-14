using System;
using System.Collections.Generic;
using UnityEngine;

namespace MusicGame.SelectMusic.Model
{
    /// <summary>
    /// 音符类型枚举（全项目统一）
    /// </summary>
    public enum NoteType
    {
        Circle,   // 基础点击
        Slider,   // 滑动
        Spinner,  // 旋转
        Hit,      // 打击
        Slide,    // 兼容旧类型
        Unknown   // 未知（扩展保底）
    }

    /// <summary>
    /// 简化颜色模型（全项目统一）
    /// </summary>
    [Serializable]
    public class SimpleColor
    {
        public float r, g, b, a;

        public SimpleColor() { }

        public SimpleColor(Color color)
        {
            r = color.r; g = color.g; b = color.b; a = color.a;
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }

    /// <summary>
    /// 音符数据模型（全项目统一）
    /// </summary>
    [Serializable]
    public class Note
    {
        public int x;
        public int y;
        public float time;
        public NoteType type;
        public float speed;        // 可选字段
        public SimpleColor color;  // 可选字段
        public float length;       // Slider 专用
        public int repeat;         // Slider 专用
        public float endTime;      // Spinner 专用
    }

    /// <summary>
    /// 单个谱面（Beatmap）
    /// </summary>
    [Serializable]
    public class Beatmap
    {
        public string difficultyName;
        public SimpleColor difficultyDisplayColor;
        public string creator;
        public string version;
        public int difficulty;
        public List<Note> noteList = new List<Note>();
    }

    /// <summary>
    /// 音乐条目（Music）
    /// </summary>
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