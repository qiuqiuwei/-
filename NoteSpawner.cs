using System.Diagnostics;
using UnityEngine;
using Model = MusicGame.SelectMusic.Model; // 统一Model命名空间
using MusicGame.Game; // 关键修复：引入NoteController所在命名空间
using UnityDebug = UnityEngine.Debug; // 解决Debug歧义

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab;
    public Vector3 judgeLinePosition = new Vector3(0, 0, 0);
    public float noteSpeed = 5f;
    private float musicStartTime;

    void Start()
    {
        musicStartTime = Time.time;

        // 测试音符生成（Model.Note来自Model命名空间）
        Model.Note testNote = new Model.Note
        {
            x = 3,
            time = 2f,
            type = Model.NoteType.Circle
        };

        SpawnNote(testNote);
    }

    // 生成音符（参数为Model.Note）
    public void SpawnNote(Model.Note noteData)
    {
        if (notePrefab == null)
        {
            UnityDebug.LogError("音符预制体未赋值！");
            return;
        }

        GameObject noteObj = Instantiate(notePrefab);
        // 关键修复：NoteController来自MusicGame.Game命名空间
        var noteController = noteObj.GetComponent<NoteController>();
        if (noteController == null)
        {
            UnityDebug.LogError("音符预制体未挂载NoteController脚本！");
            Destroy(noteObj);
            return;
        }

        // 计算生成位置（按轨道编号分配X坐标）
        Vector3 spawnPos = new Vector3(GetTrackXPosition(noteData.x), 0, judgeLinePosition.z + 10f);
        noteObj.transform.position = spawnPos;

        // 初始化音符控制器
        noteController.Initialize(
            noteData: noteData,
            musicStart: musicStartTime,
            speed: noteSpeed,
            targetY: judgeLinePosition.y,
            callback: OnNoteJudged
        );
    }

    // 根据轨道编号获取X坐标
    private float GetTrackXPosition(int trackNumber)
    {
        return trackNumber switch
        {
            1 => -3f,
            2 => -2f,
            3 => -1f,
            4 => 0f,
            5 => 1f,
            6 => 2f,
            7 => 3f,
            _ => 0f
        };
    }

    // 音符判定回调
    private void OnNoteJudged(int trackIndex, float accuracy)
    {
        string judgeResult = accuracy switch
        {
            < 0.1f => "Perfect",
            < 0.3f => "Good",
            < 0.5f => "Bad",
            _ => "Miss"
        };
        UnityDebug.Log($"轨道 {trackIndex + 1}：{judgeResult}（精度：{accuracy:F3}秒）");
    }
}