using System.Diagnostics;
using UnityEngine;
using CoreNote = MusicGame.Core.Note; // 明确引用正确的Note类型命名空间
using MusicGame.Game;
using UnityDebug = UnityEngine.Debug;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab;
    public Vector3 judgeLinePosition = new Vector3(0, 0, 0);
    public float noteSpeed = 5f;
    private float musicStartTime;

    void Start()
    {
        musicStartTime = Time.time;

        // 初始化测试音符（使用CoreNote类型）
        CoreNote testNote = new CoreNote
        {
            x = 3,
            time = 2f,
            type = CoreNote.NoteType.Circle // 确保枚举名称与Core.Note一致
        };

        SpawnNote(testNote);
    }

    // 生成音符（参数为CoreNote类型）
    public void SpawnNote(CoreNote noteData)
    {
        if (notePrefab == null)
        {
            UnityDebug.LogError("音符预制体未赋值！");
            return;
        }

        GameObject noteObj = Instantiate(notePrefab);
        NoteController noteController = noteObj.GetComponent<NoteController>();
        if (noteController == null)
        {
            UnityDebug.LogError("音符预制体未挂载NoteController脚本！");
            Destroy(noteObj);
            return;
        }

        // 计算生成位置
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