using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityWard.EventSystem {
  public abstract class Timer {
    protected float _initialTime;
    protected float _time { get; set; }
    public bool IsRunning { get; protected set; }
    public float Progress => _time / _initialTime;
    public Action OnTimerStart = delegate { };
    public Action OnTimerStop = delegate { };

    protected Timer(float value) {
      _initialTime = value;
      IsRunning = false;
      _time = _initialTime;
    }

    public void Start() {
      _time = _initialTime;
      if (!IsRunning) {
        IsRunning = true;
        OnTimerStart.Invoke();
      }
    }

    public void Stop() {
      if (IsRunning) {
        IsRunning = false;
        OnTimerStop.Invoke();
      }
    }

    public void Resume() => IsRunning = true;
    public void Pause() => IsRunning = false;

    public abstract void Tick(float deltaTime);

    public static void HandleTimers(List<Timer> timers) {
      foreach (var timer in timers) {
        timer.Tick(Time.deltaTime);
      }
    }
  }

  public class CountdownTimer : Timer {
    public CountdownTimer(float value) : base(value) { }

    public override void Tick(float deltaTime) {
      if (IsRunning && _time > 0) {
        _time -= deltaTime;
      }
      if (IsRunning && _time <= 0) {
        Stop();
      }
    }

    public bool IsFinished => _time <= 0;
    public void Reset() => _time = _initialTime;

    public void Reset(float newTime) {
      _initialTime = newTime;
      Reset();
    }
  }

  public class StopwatchTimer : Timer {
    public StopwatchTimer(float value) : base(0) { }

    public override void Tick(float deltaTime) {
      if (IsRunning) {
        _time += deltaTime;
      }
    }
    public void Reset() => _time = 0;
    public float GetTime() => _time;
  }
}