using System;
using UnityEngine;
using Model = MusicGame.SelectMusic.Model; // 引入Model命名空间
using UnityDebug = UnityEngine.Debug; // 解决Debug歧义

namespace MusicGame.Game.Custom
{
    public class CustomNoteController : MonoBehaviour
    {
        // 关键修复：明确声明所有必要变量
        private Model.Note noteData;
        private float musicStartTime; // 音符对应的音乐开始时间
        private float moveSpeed;      // 音符移动速度
        private float targetY;       // 判定线Y坐标
        private Action<int, float> judgeCallback; // 判定回调（轨道索引+精度）

        // 初始化方法：给所有变量赋值
        public void Initialize(
            Model.Note noteData,
            float musicStartTime,
            float moveSpeed,
            float targetY,
            Action<int, float> judgeCallback
        )
        {
            // 空值校验
            if (noteData == null)
            {
                UnityDebug.LogError("Initialize失败：noteData为空");
                return;
            }
            if (judgeCallback == null)
            {
                UnityDebug.LogError("Initialize失败：judgeCallback未绑定");
                return;
            }
            if (moveSpeed <= 0)
            {
                UnityDebug.LogError("Initialize失败：moveSpeed必须为正数");
                return;
            }

            // 关键修复：给变量赋值（与参数一一对应）
            this.noteData = noteData;
            this.musicStartTime = musicStartTime;
            this.moveSpeed = moveSpeed;
            this.targetY = targetY;
            this.judgeCallback = judgeCallback;
        }

        private void Update()
        {
            // 未初始化则不执行逻辑（双重保险）
            if (noteData == null || judgeCallback == null || moveSpeed <= 0)
                return;

            // 音符移动（沿Y轴向下）
            transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);
            // 判定检测
            CheckJudgement();
        }

        private void CheckJudgement()
        {
            if (noteData == null || judgeCallback == null)
            {
                Destroy(gameObject);
                return;
            }

            float currentY = transform.position.y;
            float distanceToJudgeLine = Mathf.Abs(currentY - targetY);

            // 判定范围内且按键正确
            if (distanceToJudgeLine < 0.1f)
            {
                bool isPressed = CheckTrackKey(noteData.x);
                if (isPressed)
                {
                    float currentTime = Time.time - musicStartTime;
                    float accuracy = Mathf.Abs(currentTime - noteData.time);
                    judgeCallback.Invoke(noteData.x - 1, accuracy); // 触发回调
                    Destroy(gameObject);
                }
            }
            // 超出最大判定范围（Miss）
            else if (currentY < targetY - 0.5f)
            {
                judgeCallback.Invoke(noteData.x - 1, 1f); // 精度1f表示Miss
                Destroy(gameObject);
            }
        }

        // 轨道按键映射（1-7对应A-G）
        private bool CheckTrackKey(int trackNumber)
        {
            return trackNumber switch
            {
                1 => Input.GetKeyDown(KeyCode.A),
                2 => Input.GetKeyDown(KeyCode.S),
                3 => Input.GetKeyDown(KeyCode.D),
                4 => Input.GetKeyDown(KeyCode.F),
                5 => Input.GetKeyDown(KeyCode.G),
                6 => Input.GetKeyDown(KeyCode.H),
                7 => Input.GetKeyDown(KeyCode.J),
                _ => false
            };
        }
    }
}