using System;
using System.Collections;
using UnityEngine;

namespace MusicGame.Audio
{
    [System.Serializable]
    public class BeatDetection : MonoBehaviour
    {
        // 节拍检测模式
        public enum BeatMode { Energy, Frequency, Both }
        // 节拍类型（支持位运算，可同时触发多种类型）
        [Flags]
        public enum BeatType { None = 0, Kick = 1 << 0, Snare = 1 << 1, HitHat = 1 << 2, Energy = 1 << 3 }
        // 事件类型（对应具体节拍）
        public enum EventType { Energy, Kick, Snare, HitHat }

        // 事件信息类（携带更多上下文）
        public class BeatEventInfo
        {
            public EventType eventType;       // 事件类型
            public BeatDetection sender;      // 发送者实例
            public float intensity;           // 节拍强度（0-1）
            public float time;                // 检测时间（Time.time）
        }

        // 回调委托（外部可注册监听）
        public delegate void BeatEventHandler(BeatEventInfo eventInfo);
        public BeatEventHandler OnBeatDetected;

        [Header("基础配置")]
        [Tooltip("节拍检测模式")]
        public BeatMode beatMode = BeatMode.Both;
        [Tooltip("关联的音频源（必填）")]
        public AudioSource audioSource;
        [Tooltip("FFT采样点数（必须为2的幂，越大精度越高但性能消耗大）")]
        [Range(256, 4096)] public int numSamples = 1024;
        [Tooltip("最低检测频率（Hz）")]
        public int minFrequency = 60;
        [Tooltip("最小节拍间隔（秒），避免短时间重复检测")]
        public float minBeatSeparation = 0.05f;

        [Header("能量检测参数")]
        [Tooltip("能量历史窗口长度（影响阈值计算）")]
        public int energyHistoryLength = 43;
        [Tooltip("能量检测灵敏度（值越小越敏感）")]
        public float energySensitivity = 1.5f;

        [Header("频率检测参数")]
        [Tooltip("频率历史窗口长度")]
        public int frequencyHistoryLength = 43;
        [Tooltip("每八度的频率分割数")]
        public int octaveDivisions = 3;
        [Tooltip("低频（底鼓）检测阈值倍数")]
        public float kickMultiplier = 2f;
        [Tooltip("中频（军鼓）检测阈值倍数")]
        public float snareMultiplier = 3f;
        [Tooltip("高频（踩镲）检测阈值倍数")]
        public float hatMultiplier = 4f;

        [Header("调试")]
        [Tooltip("是否输出调试日志")]
        public bool debugLogs = true;

        // 内部变量
        private float[] spectrumLeft;      // 左声道频谱数据
        private float[] spectrumRight;     // 右声道频谱数据
        private float[] outputLeft;        // 左声道输出数据
        private float[] outputRight;       // 右声道输出数据

        private float sampleRate;          // 采样率
        private int octaves;               // 频率八度数量
        private int totalFreqBands;        // 总频率带数量

        // 能量检测相关
        private float[] energyHistory;     // 能量历史记录
        private float[] energyDiffs;       // 能量差异历史
        private int energyHistoryIndex;    // 能量历史循环索引
        private int energyHistoryCount;    // 已记录的能量数量

        // 频率检测相关
        private float[,] freqHistory;      // 频率带能量历史
        private float[,] freqDiffs;        // 频率带差异历史
        private bool[] isFreqDetected;     // 各频率带是否检测到节拍
        private int freqHistoryIndex;      // 频率历史循环索引
        private int freqHistoryCount;      // 已记录的频率数量

        // 时间戳（防止短时间重复检测）
        private float lastEnergyBeatTime;
        private float[] lastFreqBeatTime;

        private void Awake()
        {
            // 初始化数组
            spectrumLeft = new float[numSamples];
            spectrumRight = new float[numSamples];
            outputLeft = new float[numSamples];
            outputRight = new float[numSamples];

            // 初始化能量检测缓冲区
            energyHistory = new float[energyHistoryLength];
            energyDiffs = new float[energyHistoryLength];

            // 自动获取AudioSource（如果未指定）
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (audioSource == null)
            {
                LogError("未找到AudioSource组件！请关联音频源。");
                enabled = false; // 禁用脚本
                return;
            }

            // 初始化采样率和频率带
            sampleRate = AudioSettings.outputSampleRate;
            CalculateFrequencyBands();

            // 初始化频率检测缓冲区
            freqHistory = new float[totalFreqBands, frequencyHistoryLength];
            freqDiffs = new float[totalFreqBands, frequencyHistoryLength];
            isFreqDetected = new bool[totalFreqBands];
            lastFreqBeatTime = new float[totalFreqBands];

            // 初始化时间戳
            lastEnergyBeatTime = -minBeatSeparation;
            for (int i = 0; i < totalFreqBands; i++)
                lastFreqBeatTime[i] = -minBeatSeparation;

            Log("节拍检测初始化完成");
        }

        private void Update()
        {
            if (audioSource == null || !audioSource.isPlaying)
                return;

            // 获取音频数据
            audioSource.GetSpectrumData(spectrumLeft, 0, FFTWindow.BlackmanHarris);
            audioSource.GetSpectrumData(spectrumRight, 1, FFTWindow.BlackmanHarris);
            audioSource.GetOutputData(outputLeft, 0);
            audioSource.GetOutputData(outputRight, 1);

            // 检测节拍并触发事件
            BeatType detectedBeats = DetectBeats();
            TriggerBeatEvents(detectedBeats);
        }

        /// <summary>
        /// 核心节拍检测逻辑
        /// </summary>
        private BeatType DetectBeats()
        {
            BeatType result = BeatType.None;

            switch (beatMode)
            {
                case BeatMode.Energy:
                    if (DetectEnergyBeat())
                        result |= BeatType.Energy;
                    break;
                case BeatMode.Frequency:
                    DetectFrequencyBeats();
                    result |= DetectKick() ? BeatType.Kick : BeatType.None;
                    result |= DetectSnare() ? BeatType.Snare : BeatType.None;
                    result |= DetectHitHat() ? BeatType.HitHat : BeatType.None;
                    break;
                case BeatMode.Both:
                    if (DetectEnergyBeat())
                        result |= BeatType.Energy;
                    DetectFrequencyBeats();
                    result |= DetectKick() ? BeatType.Kick : BeatType.None;
                    result |= DetectSnare() ? BeatType.Snare : BeatType.None;
                    result |= DetectHitHat() ? BeatType.HitHat : BeatType.None;
                    break;
            }

            return result;
        }

        /// <summary>
        /// 触发节拍事件
        /// </summary>
        private void TriggerBeatEvents(BeatType beats)
        {
            if (OnBeatDetected == null) return;

            if ((beats & BeatType.Energy) != 0)
                SendEvent(EventType.Energy, GetEnergyIntensity());
            if ((beats & BeatType.Kick) != 0)
                SendEvent(EventType.Kick, GetKickIntensity());
            if ((beats & BeatType.Snare) != 0)
                SendEvent(EventType.Snare, GetSnareIntensity());
            if ((beats & BeatType.HitHat) != 0)
                SendEvent(EventType.HitHat, GetHatIntensity());
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        private void SendEvent(EventType type, float intensity)
        {
            BeatEventInfo eventInfo = new BeatEventInfo
            {
                eventType = type,
                sender = this,
                intensity = Mathf.Clamp01(intensity),
                time = Time.time
            };
            OnBeatDetected(eventInfo);
            Log($"检测到节拍：{type}，强度：{intensity:F2}");
        }

        // -------------------------- 能量检测逻辑 --------------------------
        private bool DetectEnergyBeat()
        {
            // 计算当前音频能量（平方和平均）
            float currentEnergy = 0;
            for (int i = 0; i < numSamples; i++)
            {
                currentEnergy += outputLeft[i] * outputLeft[i];
                currentEnergy += outputRight[i] * outputRight[i];
            }
            currentEnergy /= numSamples;
            currentEnergy = Mathf.Sqrt(currentEnergy) * 100; // 归一化

            // 计算历史能量平均值和方差
            float avgEnergy = CalculateAverage(energyHistory, energyHistoryCount);
            float energyVariance = CalculateVariance(energyHistory, energyHistoryCount, avgEnergy);

            // 动态阈值计算（基于方差调整灵敏度）
            float threshold = energySensitivity * avgEnergy;
            float diff = Mathf.Max(currentEnergy - threshold, 0);

            // 检查是否超过最小间隔
            bool isBeat = (Time.time - lastEnergyBeatTime >= minBeatSeparation)
                        && (currentEnergy > threshold)
                        && (currentEnergy > 2f); // 基础能量阈值

            // 更新历史记录
            UpdateHistoryBuffer(energyHistory, ref energyHistoryIndex, ref energyHistoryCount, currentEnergy, energyHistoryLength);
            UpdateHistoryBuffer(energyDiffs, ref energyHistoryIndex, ref energyHistoryCount, diff, energyHistoryLength);

            if (isBeat)
                lastEnergyBeatTime = Time.time;

            return isBeat;
        }

        // -------------------------- 频率检测逻辑 --------------------------
        private void CalculateFrequencyBands()
        {
            // 计算八度数量（基于采样率和最小频率）
            float nyquist = sampleRate / 2f; // 奈奎斯特频率
            octaves = 1;
            while ((nyquist /= 2) > minFrequency)
                octaves++;

            totalFreqBands = octaves * octaveDivisions;
            Log($"频率带初始化完成：八度={octaves}，每八度分割={octaveDivisions}，总带数={totalFreqBands}");
        }

        private void DetectFrequencyBeats()
        {
            // 计算每个频率带的平均能量
            float[] bandAverages = new float[totalFreqBands];
            CalculateFrequencyBandAverages(bandAverages);

            // 逐个频率带检测节拍
            for (int i = 0; i < totalFreqBands; i++)
            {
                float current = bandAverages[i];
                float avg = CalculateAverage(freqHistory, i, freqHistoryCount);
                float variance = CalculateVariance(freqHistory, i, freqHistoryCount, avg);

                // 根据频率带位置选择阈值倍数
                float multiplier = GetFrequencyMultiplier(i);
                float threshold = multiplier * avg;
                float diff = Mathf.Max(current - threshold, 0);

                // 检查是否超过最小间隔和基础阈值
                bool detected = (Time.time - lastFreqBeatTime[i] >= minBeatSeparation)
                              && (current > threshold)
                              && (current > GetFrequencyThreshold(i));

                // 更新历史和状态
                UpdateFrequencyHistory(i, current, diff);
                isFreqDetected[i] = detected;

                if (detected)
                    lastFreqBeatTime[i] = Time.time;
            }
        }

        /// <summary>
        /// 计算每个频率带的平均能量
        /// </summary>
        private void CalculateFrequencyBandAverages(float[] averages)
        {
            float nyquist = sampleRate / 2f;
            for (int i = 0; i < octaves; i++)
            {
                // 计算当前八度的频率范围
                float lowFreq = (i == 0) ? 0f : (nyquist / Mathf.Pow(2, octaves - i));
                float highFreq = nyquist / Mathf.Pow(2, octaves - i - 1);
                float freqStep = (highFreq - lowFreq) / octaveDivisions;

                // 计算每个子带的平均能量
                for (int j = 0; j < octaveDivisions; j++)
                {
                    int bandIndex = j + i * octaveDivisions;
                    float currentLow = lowFreq + j * freqStep;
                    float currentHigh = currentLow + freqStep;

                    // 取左右声道的最大值
                    float leftAvg = CalculateFrequencyRangeAverage(currentLow, currentHigh, spectrumLeft);
                    float rightAvg = CalculateFrequencyRangeAverage(currentLow, currentHigh, spectrumRight);
                    averages[bandIndex] = Mathf.Max(leftAvg, rightAvg);
                }
            }
        }

        /// <summary>
        /// 计算指定频率范围内的平均能量
        /// </summary>
        private float CalculateFrequencyRangeAverage(float lowFreq, float highFreq, float[] spectrum)
        {
            int startIndex = FrequencyToSpectrumIndex(lowFreq);
            int endIndex = FrequencyToSpectrumIndex(highFreq);
            startIndex = Mathf.Clamp(startIndex, 0, spectrum.Length - 1);
            endIndex = Mathf.Clamp(endIndex, startIndex, spectrum.Length - 1);

            float sum = 0;
            for (int i = startIndex; i <= endIndex; i++)
                sum += spectrum[i];
            return sum / (endIndex - startIndex + 1);
        }

        /// <summary>
        /// 将频率转换为频谱数组索引
        /// </summary>
        private int FrequencyToSpectrumIndex(float freq)
        {
            float bandwidth = sampleRate / numSamples;
            if (freq < bandwidth / 2) return 0;
            if (freq > sampleRate / 2 - bandwidth / 2) return numSamples / 2 - 1;
            return Mathf.Clamp((int)(numSamples * (freq / sampleRate)), 0, numSamples - 1);
        }

        // -------------------------- 特定乐器检测 --------------------------
        private bool DetectKick()
        {
            // 底鼓：低频带（1-6号带）
            int upper = Mathf.Min(6, totalFreqBands - 1);
            return CheckFrequencyRange(1, upper, 2);
        }

        private bool DetectSnare()
        {
            // 军鼓：中频段（8号带至中间）
            int lower = Mathf.Min(8, totalFreqBands - 1);
            int upper = totalFreqBands - 5;
            if (upper < lower) upper = lower;
            int threshold = (upper - lower) / 3;
            return CheckFrequencyRange(lower, upper, threshold);
        }

        private bool DetectHitHat()
        {
            // 踩镲：高频带（最后6个带）
            int lower = Mathf.Max(0, totalFreqBands - 6);
            int upper = totalFreqBands - 1;
            return CheckFrequencyRange(lower, upper, 1);
        }

        /// <summary>
        /// 检查指定频率范围内是否有足够的节拍
        /// </summary>
        private bool CheckFrequencyRange(int low, int high, int minDetected)
        {
            int count = 0;
            for (int i = low; i <= high; i++)
            {
                if (i >= totalFreqBands) break;
                if (isFreqDetected[i]) count++;
            }
            return count >= minDetected;
        }

        // -------------------------- 工具方法 --------------------------
        /// <summary>
        /// 更新历史缓冲区
        /// </summary>
        private void UpdateHistoryBuffer(float[] buffer, ref int index, ref int count, float value, int maxLength)
        {
            buffer[index] = value;
            index = (index + 1) % maxLength;
            if (count < maxLength) count++;
        }

        /// <summary>
        /// 更新频率带历史
        /// </summary>
        private void UpdateFrequencyHistory(int bandIndex, float value, float diff)
        {
            freqHistory[bandIndex, freqHistoryIndex] = value;
            freqDiffs[bandIndex, freqHistoryIndex] = diff;
            freqHistoryIndex = (freqHistoryIndex + 1) % frequencyHistoryLength;
            if (freqHistoryCount < frequencyHistoryLength) freqHistoryCount++;
        }

        /// <summary>
        /// 计算数组平均值
        /// </summary>
        private float CalculateAverage(float[] array, int count)
        {
            if (count == 0) return 0;
            float sum = 0;
            for (int i = 0; i < count; i++) sum += array[i];
            return sum / count;
        }

        /// <summary>
        /// 计算频率带历史平均值
        /// </summary>
        private float CalculateAverage(float[,] array, int band, int count)
        {
            if (count == 0) return 0;
            float sum = 0;
            for (int i = 0; i < count; i++) sum += array[band, i];
            return sum / count;
        }

        /// <summary>
        /// 计算方差
        /// </summary>
        private float CalculateVariance(float[] array, int count, float average)
        {
            if (count <= 1) return 0;
            float sum = 0;
            for (int i = 0; i < count; i++) sum += Mathf.Pow(array[i] - average, 2);
            return sum / count;
        }

        /// <summary>
        /// 计算频率带方差
        /// </summary>
        private float CalculateVariance(float[,] array, int band, int count, float average)
        {
            if (count <= 1) return 0;
            float sum = 0;
            for (int i = 0; i < count; i++) sum += Mathf.Pow(array[band, i] - average, 2);
            return sum / count;
        }

        /// <summary>
        /// 根据频率带获取阈值倍数
        /// </summary>
        private float GetFrequencyMultiplier(int bandIndex)
        {
            if (bandIndex < 7) return kickMultiplier;       // 低频（底鼓）
            else if (bandIndex < 20) return snareMultiplier; // 中频（军鼓）
            else return hatMultiplier;                       // 高频（踩镲）
        }

        /// <summary>
        /// 根据频率带获取基础阈值
        /// </summary>
        private float GetFrequencyThreshold(int bandIndex)
        {
            if (bandIndex < 7) return 0.003f;    // 底鼓阈值
            else if (bandIndex < 20) return 0.001f; // 军鼓阈值
            else return 0.001f;                  // 踩镲阈值
        }

        // -------------------------- 强度计算（用于事件） --------------------------
        private float GetEnergyIntensity()
        {
            float avg = CalculateAverage(energyHistory, energyHistoryCount);
            return avg > 0 ? energyHistory[energyHistoryIndex] / avg : 0;
        }

        private float GetKickIntensity()
        {
            return CalculateRangeIntensity(1, Mathf.Min(6, totalFreqBands - 1));
        }

        private float GetSnareIntensity()
        {
            int lower = Mathf.Min(8, totalFreqBands - 1);
            int upper = totalFreqBands - 5;
            return CalculateRangeIntensity(lower, upper);
        }

        private float GetHatIntensity()
        {
            int lower = Mathf.Max(0, totalFreqBands - 6);
            int upper = totalFreqBands - 1;
            return CalculateRangeIntensity(lower, upper);
        }

        private float CalculateRangeIntensity(int low, int high)
        {
            float sum = 0;
            int count = 0;
            for (int i = low; i <= high; i++)
            {
                if (i >= totalFreqBands) break;
                sum += freqHistory[i, freqHistoryIndex];
                count++;
            }
            return count > 0 ? sum / count : 0;
        }

        // -------------------------- 日志方法 --------------------------
        private void Log(string message)
        {
            if (debugLogs)
                UnityEngine.Debug.Log($"[BeatDetection] {message}");
        }

        private void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning($"[BeatDetection] {message}");
        }

        private void LogError(string message)
        {
            UnityEngine.Debug.LogError($"[BeatDetection] {message}");
        }
    }
}