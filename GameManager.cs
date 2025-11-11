using MusicGame.Core;
using MusicGame.SelectMusic; // 关键修复：添加RuntimeData所在命名空间
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityDebug = UnityEngine.Debug; // 解决Debug歧义
using Utils = MusicGame.SelectMusic.Utils; // 明确Utils命名空间

namespace MusicGame.Game
{
    public class GameManager : MonoBehaviour
    {
        [Header("游戏资源引用")]
        [SerializeField] private AudioSource musicAudioSource;
        [SerializeField] private GameObject notePrefab;
        [SerializeField] private Transform[] noteSpawnPoints;
        [SerializeField] private Transform judgeLine;

        [Header("游戏参数设置")]
        [Range(3f, 10f)]
        [SerializeField] private float noteSpeed = 5f;

        [Header("判定阈值(秒)")]
        [SerializeField] private float perfectThreshold = 0.1f;
        [SerializeField] private float greatThreshold = 0.2f;
        [SerializeField] private float goodThreshold = 0.3f;

        private Music selectedMusic;
        private Beatmap selectedBeatmap;
        private List<Note> activeNotes = new List<Note>();
        private float musicStartTime;
        private float judgeLineY;

        private void Start()
        {
            if (judgeLine == null)
            {
                UnityDebug.LogError("[GameManager] 未赋值判定线(judgeLine)");
                return;
            }
            judgeLineY = judgeLine.position.y;

            LoadSelectedData();

            if (selectedMusic != null && selectedBeatmap != null)
            {
                LoadAndPlayMusic();
                ParseAndSpawnNotes();
            }
        }

        private void LoadSelectedData()
        {
            // 关键修复：RuntimeData来自MusicGame.SelectMusic命名空间
            selectedMusic = RuntimeData.selectedMusic;
            selectedBeatmap = RuntimeData.selectedBeatmap;

            if (selectedMusic == null)
            {
                UnityDebug.LogError("[GameManager] 未获取到选中的音乐数据");
                BackToSelectScene();
                return;
            }

            if (selectedBeatmap == null || selectedBeatmap.noteList == null || selectedBeatmap.noteList.Count == 0)
            {
                UnityDebug.LogError("[GameManager] 选中的谱面数据无效");
                BackToSelectScene();
                return;
            }

            if (noteSpawnPoints == null || noteSpawnPoints.Length != 7)
            {
                UnityDebug.LogError("[GameManager] 未设置7个音符生成点");
                BackToSelectScene();
            }
        }

        private void LoadAndPlayMusic()
        {
            if (musicAudioSource == null)
            {
                UnityDebug.LogError("[GameManager] 未赋值音乐播放器");
                return;
            }

            var musicClip = Utils.LoadAudio(selectedMusic.audioFilename);
            if (musicClip == null)
            {
                UnityDebug.LogError($"[GameManager] 加载音乐失败: {selectedMusic.audioFilename}");
                BackToSelectScene();
                return;
            }

            musicAudioSource.clip = musicClip;
            musicAudioSource.Play();
            musicStartTime = Time.time;
            UnityDebug.Log($"[GameManager] 开始播放: {selectedMusic.title} (时长: {musicClip.length:F2}秒)");
        }

        private void ParseAndSpawnNotes()
        {
            if (notePrefab == null)
            {
                UnityDebug.LogError("[GameManager] 未赋值音符预制体");
                return;
            }

            foreach (var noteData in selectedBeatmap.noteList)
            {
                float spawnDelay = noteData.time - (Time.time - musicStartTime);

                if (spawnDelay > 0)
                {
                    StartCoroutine(SpawnNoteAfterDelay(noteData, spawnDelay));
                }
                else if (spawnDelay > -0.5f)
                {
                    SpawnNote(noteData);
                }
                else
                {
                    UnityDebug.LogWarning($"[GameManager] 音符已过期: {noteData.time}秒");
                }
            }
        }

        private IEnumerator SpawnNoteAfterDelay(Note noteData, float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnNote(noteData);
        }

        private void SpawnNote(Note noteData)
        {
            int trackIndex = Mathf.Clamp(noteData.x - 1, 0, noteSpawnPoints.Length - 1);
            var spawnPoint = noteSpawnPoints[trackIndex];

            if (spawnPoint == null)
            {
                UnityDebug.LogError($"[GameManager] 轨道 {trackIndex + 1} 未设置生成点");
                return;
            }

            var noteObj = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
            var noteCtrl = noteObj.GetComponent<NoteController>();

            if (noteCtrl != null)
            {
                noteCtrl.Initialize(
                    noteData: noteData,
                    musicStartTime: musicStartTime,
                    moveSpeed: noteSpeed,
                    targetY: judgeLineY,
                    judgeCallback: OnNoteJudged
                );
                activeNotes.Add(noteData);
            }
            else
            {
                UnityDebug.LogError("[GameManager] 音符预制体缺少NoteController组件");
                Destroy(noteObj);
            }
        }

        private void OnNoteJudged(int trackIndex, float accuracy)
        {
            string judgeResult = GetJudgeResult(accuracy);
            UnityDebug.Log($"[判定] 轨道 {trackIndex + 1}: {judgeResult} (精度: {accuracy:F3}秒)");
        }

        private string GetJudgeResult(float accuracy)
        {
            if (accuracy <= perfectThreshold) return "Perfect";
            if (accuracy <= greatThreshold) return "Great";
            if (accuracy <= goodThreshold) return "Good";
            return "Miss";
        }

        private void BackToSelectScene()
        {
            StartCoroutine(DelayLoadScene("SelectMusic", 1f));
        }

        private IEnumerator DelayLoadScene(string sceneName, float delay)
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(sceneName);
        }
    }
}