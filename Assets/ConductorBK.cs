using System;
using UnityEngine;

public class ConductorBK : MonoBehaviour
{
    #region Static
    static double _accurateTime = float.NaN;
    static double _smoothedTime = float.NaN;

    public static float AudioLatency { get; set; }

    public static float AccurateTime { get => (float)_accurateTime - AudioLatency; }
    public static float SmoothedTime { get => (float)_smoothedTime - AudioLatency; }
    #endregion

    AudioSource _source;
    int _lastSamplePosition;
    double _timeToMakeUpAccurate;
    double _timeToMakeUpSmoothed;

    void Start()
    {
        _source = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (_source == null || _source.clip == null || !_source.isPlaying)
        {
            _accurateTime = 0;
            _smoothedTime = 0;
            return;
        }

        // Interpolations must run every frame, even if they are being
        // synced this frame.
        InterpolateAccurateTime();
        InterpolateSmoothedTime();

        // Sync only when the play head has updated.
        if (_source.timeSamples != _lastSamplePosition)
        {
            // Calculate real time position;
            double real = (double)_source.timeSamples / _source.clip.frequency;
            _lastSamplePosition = _source.timeSamples;

            // On the first update.
            if (_lastSamplePosition == 0)
            {
                _accurateTime = real;
                _smoothedTime = real;
            }

            SyncAccurateTime(real);
            SyncSmoothedTime(real);
        }
    }

    /// <summary>
    /// Interpolates the accurate time whilst embracing the jitter.
    /// </summary>
    void InterpolateAccurateTime()
    {
        // If the time is ahead, pause.
        if (_timeToMakeUpAccurate > 0)
        {
            _timeToMakeUpAccurate -= Time.deltaTime;
            if (_timeToMakeUpAccurate <= 0)
            {
                _accurateTime = Math.Abs(_timeToMakeUpAccurate);
                _timeToMakeUpAccurate = 0;
            }
        }
        else
        {
            _accurateTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// Interpolates the smooth time while reducing jitter.
    /// </summary>
    void InterpolateSmoothedTime()
    {
        _smoothedTime += Time.deltaTime;

        float correction = 0;
        if (_timeToMakeUpSmoothed > 0)
        {
            if (_timeToMakeUpSmoothed < 0.01)
                correction = Time.deltaTime * 0.1f;
            else
                correction = Time.deltaTime * 0.2f;
        }
        else if (_timeToMakeUpSmoothed < 0)
        {
            if (_timeToMakeUpSmoothed > -0.01)
                correction = -Time.deltaTime * 0.1f;
            else
                correction = -Time.deltaTime * 0.2f;
        }

        _timeToMakeUpSmoothed -= correction;
        _smoothedTime += correction;
    }

    /// <summary>
    /// Brings accurate time back in sync without backtracking.
    /// </summary>
    /// <param name="realTime"></param>
    void SyncAccurateTime(double realTime)
    {
        if (realTime < _accurateTime)
        {
            _timeToMakeUpAccurate = _accurateTime - realTime;
        }
        else
        {
            _accurateTime = realTime;
        }
    }

    /// <summary>
    /// Brings smooth time back in sync without backtracking.
    /// </summary>
    /// <param name="realTime"></param>
    void SyncSmoothedTime(double realTime)
    {
        _timeToMakeUpSmoothed = realTime - _smoothedTime;
        if (Math.Abs(_timeToMakeUpSmoothed) > 0.05)
        {
            _smoothedTime = realTime;
            _timeToMakeUpSmoothed = 0;
        }
    }

    #region Public
    /// <summary>
    /// Sets a new audio clip.
    /// </summary>
    /// <param name="clip"></param>
    public void SetAudioClip(AudioClip clip)
    {
        _source.clip = clip;
        OverridePlaybackTime(0);
    }

    /// <summary>
    /// Clears the audio clip from the audio source.
    /// </summary>
    public void UnloadAudioClip()
    {
        _source.clip = null;
        _accurateTime = double.NaN;
        _smoothedTime = double.NaN;
    }

    /// <summary>
    /// Starts audio playback from the given time.
    /// </summary>
    public void Play(float time = 0)
    {
        OverridePlaybackTime(time);
        _source.Play();
    }

    /// <summary>
    /// Pauses the audio playback.
    /// </summary>
    public void Pause()
    {
        _source.Pause();
    }

    /// <summary>
    /// Resumes audio playback.
    /// </summary>
    public void Resume()
    {
        _source.UnPause();
    }

    /// <summary>
    /// Stops audio playback.
    /// </summary>
    public void Stop()
    {
        _source.Stop();
    }
    #endregion

    /// <summary>
    /// Sets the playback position to the given time.
    /// </summary>
    /// <param name="time">Seconds</param>
    void OverridePlaybackTime(float time)
    {
        // Calculate how many samples that translates to.
        _source.timeSamples = (int)(time * _source.clip.frequency);
        // The actual time it was set to.
        double actualTime = (double)_source.timeSamples / _source.clip.frequency;

        // Update remaining values.
        _accurateTime = actualTime;
        _smoothedTime = actualTime;
        _timeToMakeUpAccurate = 0;
        _timeToMakeUpSmoothed = 0;
        _lastSamplePosition = _source.timeSamples;
    }
}
