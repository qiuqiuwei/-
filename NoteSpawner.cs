using UnityEngine;
using Model = MusicGame.SelectMusic.Model;
using UnityDebug = UnityEngine.Debug;
using MusicGame.Game;

public class NoteSpawner : MonoBehaviour
{
    public GameObject notePrefab;
    public Vector3 judgeLinePosition = new Vector3(0, 0, 0);
    public float noteSpeed = 5f;
    private float musicStartTime;

    void Start()
    {
        musicStartTime = Time.time;

        // 示例：使用 Model.Note 生成一个测试音符
        Model.Note testNote = new Model.Note
        {
            x = 3,
            time = 2f,
            type = Model.NoteType.Circle
        };

        SpawnNote(testNote);
    }

    // 使用 Model.Note 作为输入类型
    public void SpawnNote(Model.Note noteData)
    {
        if (notePrefab == null)
        {
            UnityDebug.LogError("notePrefab 未赋值");
            return;
        }

        GameObject noteObj = Instantiate(notePrefab);
        NoteController noteController = noteObj.GetComponent<NoteController>();
        if (noteController == null)
        {
            UnityDebug.LogError("notePrefab 缺少 NoteController 组件");
            Destroy(noteObj);
            return;
        }

        Vector3 spawnPos = new Vector3(GetTrackXPosition(noteData.x), 0, judgeLinePosition.z + 10f);
        noteObj.transform.position = spawnPos;

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
        UnityDebug.Log($"轨道 {trackIndex + 1} 判定: {judgeResult} 精度: {accuracy:F3}s");
    }
}