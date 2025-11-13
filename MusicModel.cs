using System;
using System.Collections.Generic;
using UnityEngine;

namespace MusicGame.SelectMusic.Model
{
    /// <summary>
    /// ��������ö�٣�ȫ��Ψһ���壩
    /// </summary>
    public enum NoteType
    {
        Circle,   // ��������
        Slider,   // ����
        Spinner,  // ��ת
        Hit,      // ����
        Slide,    // ���ݾ�����
        Unknown   // δ֪���ͣ���ѡ��չ��
    }

    /// <summary>
    /// ������ɫģ�ͣ�ȫ��Ψһ���壬ȷ�����л�����Ψһ��
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
    /// ��������ģ�ͣ�ȫ��Ψһ���壬ȷ�����л�����Ψһ��
    /// </summary>
    [Serializable]
    public class Note
    {
        public int x;
        public int y;
        public float time;
        public NoteType type;
        public float speed;        // �����ƶ��ٶȣ���Coreģ�����ݣ�
        public SimpleColor color;  // ������ʾ��ɫ����Coreģ�����ݣ�
        public float length;       // ����/�������ȣ�Sliderר�ã�
        public int repeat;         // �����ظ�������Sliderר�ã�
        public float endTime;      // ����ʱ�䣨Spinnerר�ã�
    }

    /// <summary>
    /// ����ģ�ͣ�ȫ��Ψһ���壬ȷ�����л�����Ψһ��
    /// </summary>
    [Serializable]
    public class Beatmap
    {
        public string difficultyName;
        public SimpleColor difficultyDisplayColor;
        public string creator;
        public string version;
        public int difficulty;     // �Ѷ���ֵ����Coreģ�����ݣ�
        public List<Note> noteList = new List<Note>();
    }

    /// <summary>
    /// ����ģ�ͣ�ȫ��Ψһ���壬ȷ�����л�����Ψһ��
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