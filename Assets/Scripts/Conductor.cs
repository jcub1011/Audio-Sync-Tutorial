using System;
using System.Diagnostics;
using UnityEngine;

namespace TimeKeeping
{
    [RequireComponent(typeof(AudioSource))]
    public class Conductor : MonoBehaviour
    {
        #region Static
        static double _smoothedTime = 0;
        public static float UnSmoothedTime { get; private set; }
        public static float SmoothedTime { get; private set; }
        public static float AudioLatency { get; set; }
        public static Conductor Instance { get; private set; }
        #endregion

        AudioSource _source;
        int _lastSamplePosition;
        double _variance;
        double _swStartTime;
        Logger.CSVGenerator _csvGenerator;
        Stopwatch _sw;

        void Start()
        {
            _source = GetComponent<AudioSource>();
            _csvGenerator = new("Time Captured", "Real Time", "Smoothed Time", "Stopwatch Time", "Smoothed Time Drift", "Stopwatch Time Drift");
            _sw = Stopwatch.StartNew();
            Instance = this;
        }

        void Update()
        {
            // If there is nothing to do, early return.
            if (_source == null || _source.clip == null)
            {
                _smoothedTime = 0;
                UnSmoothedTime = 0;
                return;
            }

            // Interpolations must run every frame, even if they are being
            // synced this frame.
            InterpolateSmoothedTime(ref _smoothedTime, ref _variance);

            // Sync only when the play head has updated.
            // We ignore 0 because if the song reaches the end, it will
            // set timeSamples to 0. We can't reach the end of the song
            // when AudioLatency is set higher than 0 if we don't ignore timeSamples == 0.
            if (_source.timeSamples != _lastSamplePosition
                && _source.timeSamples != 0)
            {
                // Calculate real time position;
                double real = (double)_source.timeSamples / _source.clip.frequency;
                _lastSamplePosition = _source.timeSamples;

                _csvGenerator.AppendRow(
                    $"{_sw.Elapsed.TotalSeconds:0.000}",
                    $"{real:0.000}", 
                    $"{_smoothedTime:0.000}", 
                    $"{_sw.Elapsed.TotalSeconds:0.000}",
                    $"{real - _smoothedTime:0.000}",
                    $"{real - (_sw.Elapsed.TotalSeconds - _swStartTime):0.000}");

                SyncSmoothedTime(real, ref _smoothedTime, out _variance);
            }

            UnSmoothedTime = _source.time - AudioLatency;
            SmoothedTime = Mathf.Clamp((float)_smoothedTime - AudioLatency, -AudioLatency, _source.clip.length);

            double difference = _sw.Elapsed.TotalSeconds - _swStartTime - UnSmoothedTime;
            if (Math.Abs(difference) > 0.200)
            {
                _swStartTime += difference;
            }

            if (UnSmoothedTime > 50 && !_csvGenerator.Exported)
            {
                _csvGenerator.Export(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "audio_sync_log");
            }
        }

        /// <summary>
        /// Interpolates the smooth time while reducing jitter.
        /// </summary>
        /// <param name="smoothedTime">The smoothed time.</param>
        /// <param name="variance">The variance from the reported time of the audio source.</param>
        void InterpolateSmoothedTime(ref double smoothedTime, ref double variance)
        {
            const float CLOSE_DRIFT_SPEED = 0.05f; // 50ms of correction per second.
            const float FAR_DRIFT_SPEED = 0.01f; // 100ms of correction per second.
            smoothedTime += Time.deltaTime;

            float correction = 0;
            if (variance > 0)
            {
                if (variance < 0.01)
                    correction = Time.deltaTime * CLOSE_DRIFT_SPEED;
                else
                    correction = Time.deltaTime * FAR_DRIFT_SPEED;
            }
            else if (variance < 0)
            {
                if (variance > -0.01)
                    correction = -Time.deltaTime * CLOSE_DRIFT_SPEED;
                else
                    correction = -Time.deltaTime * FAR_DRIFT_SPEED;
            }

            variance -= correction;
            smoothedTime += correction;
        }

        /// <summary>
        /// Brings smooth time back in sync without backtracking.
        /// </summary>
        /// <param name="realTime"></param>
        /// <param name="smoothedTime">The current smoothed time.</param>
        /// <param name="variance">The variance fromt he real time.</param>
        void SyncSmoothedTime(double realTime, ref double smoothedTime, out double variance)
        {
            variance = realTime - smoothedTime;

            // Variance greater than 30 ms is very bad.
            // We just take the L and create jitter.
            if (Math.Abs(variance) > 0.03)
            {
                smoothedTime = realTime;
                variance = 0;
            }
        }
    }
}
