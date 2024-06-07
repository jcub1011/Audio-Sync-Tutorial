using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using TimeKeeping;

public class Mover : MonoBehaviour
{
    public float _time;
    public double _startTime;
    public long _startTimeMS;
    Vector3 _startPos;
    Vector3 _endPos;
    Stopwatch _sw;

    private void Start()
    {
        _startPos = transform.position;
        _endPos = transform.position + Vector3.right * 13;
        _startTime = AudioSettings.dspTime;
        _sw = Stopwatch.StartNew();
        _startTimeMS = _sw.ElapsedMilliseconds;
    }

    // Update is called once per frame
    void Update()
    {
        double stopwatchTime = (double)(_sw.ElapsedMilliseconds - _startTimeMS) / 1000;
        float t = Conductor.ReportedTime % _time / _time;
        print($"Song Time: {Conductor.ReportedTime:0.000}s\n" +
           $"Audio DSP Time: {AudioSettings.dspTime - _startTime:0.000}s\n" +
           $"Stopwatch Time: {stopwatchTime:0.000}s");

        transform.position = Vector3.Lerp(_startPos, _endPos, t);
    }
}
