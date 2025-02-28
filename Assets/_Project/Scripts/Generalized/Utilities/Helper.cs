using MEC;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace RealityWard.Utilities.Helpers {
  public static class Helpers {
    static string _targetSceneName;

    public static void QuitGame() {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    public static void LoadScene(string sceneName) {
      _targetSceneName = sceneName;
      SceneManager.LoadScene("Loading");
      Timing.RunCoroutine(LoadTargetScene());
    }

    private static IEnumerator<float> LoadTargetScene() {
      yield return Timing.WaitForOneFrame;
      SceneManager.LoadScene(_targetSceneName);
    }
  }
}