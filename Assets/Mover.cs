using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using TimeKeeping;

public class Mover : MonoBehaviour
{
    public float _time;
    public bool _useSmoothedTime = false;
    Vector3 _startPos;
    Vector3 _endPos;

    private void Start()
    {
        _startPos = transform.position;
        _endPos = transform.position + Vector3.right * 13;
    }

    // Update is called once per frame
    void Update()
    {
        float time = _useSmoothedTime ? Conductor.SmoothedTime : Conductor.UnSmoothedTime;
        float t = time % _time / _time;
        transform.position = Vector3.Lerp(_startPos, _endPos, t);
    }
}
