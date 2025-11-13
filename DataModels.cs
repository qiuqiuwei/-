namespace MusicGame.SelectMusic.Model
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 音符类型枚举（选曲模块）
    /// 与 Core.NoteType 完全一致，确保数据兼容性
    /// </summary>
    public enum NoteType
    {
        Circle,   // 基础点击
        Slider,   // 滑动
        Spinner,  // 旋转
        Hit,      // 打击
        Slide     // 滑动（兼容旧类型）
    }

    /// <summary>
    /// 音符数据模型（选曲模块）
    /// 与 Core.Note 字段一一对应，用于选曲阶段的谱面解析和展示
    /// </summary>
    [Serializable]
    public class Note
    {
        public NoteType type;               // 音符类型（关联当前命名空间的 NoteType）
        public float time;                  // 音符出现时间（秒）
        public float speed;                 // 音符移动速度
        public SimpleColor color;           // 音符显示颜色
        public int x;                       // 轨道X坐标
        public int y;                       // 轨道Y坐标
        public float length;                // 滑动音符长度（Slider专用）
        public int repeat;                  // 滑动重复次数（Slider专用）
        public float endTime;               // 结束时间（Spinner专用）
    }

    /// <summary>
    /// 简化颜色模型（选曲模块）
    /// 与 Core.SimpleColor 结构和方法一致，支持颜色转换
    /// </summary>
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