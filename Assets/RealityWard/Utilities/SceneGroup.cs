using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RealityWard.Utilities.SceneManagement {
  [Serializable]
  public class SceneGroup {
    public string Name = "New Scene Group";
    public List<SceneData> Scenes;
    public string FindSceneNameByType(SceneType sceneType) {
      return Scenes.FirstOrDefault(scene => scene.SceneType == sceneType)?.Name;
    }
  }

  [Serializable]
  public class SceneData {
    public string Name = "";
    public SceneType SceneType;
  }

  public enum SceneType { ActiveScene, MainMenu, UserInterface, HUD, Cinematic, Environment, Tooling }

  public class LoadingProgress : IProgress<float> {
    public event Action<float> Progressed;
    const float _Ratio = 1f;
    public void Report(float value) {
      Progressed?.Invoke(value / _Ratio);
    }
  }

  public readonly struct AsyncOperationGroup {
    public readonly List<AsyncOperation> Operations;

    public float Progress => Operations.Count == 0 ? 0 : Operations.Average(o => o.progress);
    public bool IsDone => Operations.All(o => o.isDone);

    public AsyncOperationGroup(int initialCapacity) {
      Operations = new List<AsyncOperation>(initialCapacity);
    }
  }
}