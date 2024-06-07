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
            InterpolateSmoothedTime();

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

                SyncSmoothedTime(real);
                _csvGenerator.AppendRow(
                    $"{_sw.Elapsed.TotalSeconds:0.000}",
                    $"{real:0.000}", 
                    $"{_smoothedTime:0.000}", 
                    $"{_sw.Elapsed.TotalSeconds:0.000}",
                    $"{real - _smoothedTime:0.000}",
                    $"{real - (_sw.Elapsed.TotalSeconds - _swStartTime):0.000}");
            }

            UnSmoothedTime = _source.time - AudioLatency;
            SmoothedTime = (float)_smoothedTime - AudioLatency;


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
        void InterpolateSmoothedTime()
        {
            const float CLOSE_DRIFT_SPEED = 0.1f; // 100ms of correction per second.
            const float FAR_DRIFT_SPEED = 0.2f; // 200ms of correction per second.
            _smoothedTime += Time.deltaTime;

            float correction = 0;
            if (_variance > 0)
            {
                if (_variance < 0.01)
                    correction = Time.deltaTime * CLOSE_DRIFT_SPEED;
                else
                    correction = Time.deltaTime * FAR_DRIFT_SPEED;
            }
            else if (_variance < 0)
            {
                if (_variance > -0.01)
                    correction = -Time.deltaTime * CLOSE_DRIFT_SPEED;
                else
                    correction = -Time.deltaTime * FAR_DRIFT_SPEED;
            }

            _variance -= correction;
            _smoothedTime += correction;
        }

        /// <summary>
        /// Brings smooth time back in sync without backtracking.
        /// </summary>
        /// <param name="realTime"></param>
        void SyncSmoothedTime(double realTime)
        {
            _variance = realTime - _smoothedTime;

            // Variance greater than 30 ms is very bad.
            // We just take the L and create jitter.
            if (Math.Abs(_variance) > 0.03)
            {
                _smoothedTime = realTime;
                _variance = 0;
            }
        }
    }
}
