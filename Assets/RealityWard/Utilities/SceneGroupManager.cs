using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealityWard.Utilities.SceneManagement {
  public class SceneGroupManager {
    public event Action<string> OnSceneLoaded = delegate { };
    public event Action<string> OnSceneUnloaded = delegate { };
    public event Action OnSceneGroupLoaded = delegate { };

    SceneGroup _activeSceneGroup;

    public async Task LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDupScenes = false) {
      _activeSceneGroup = group;
      var loadedScenes = new List<string>();

      await UnloadScenes();

      int sceneCount = SceneManager.sceneCount;
      for (var i = 0; i < sceneCount; i++) {
        loadedScenes.Add(SceneManager.GetSceneAt(i).name);
      }

      var totalScenesToLoad = _activeSceneGroup.Scenes.Count;
      var operationsGroup = new AsyncOperationGroup(totalScenesToLoad);

      for (int i = 0; i < totalScenesToLoad; i++) {
        var sceneData = group.Scenes[i];
        if (!reloadDupScenes && loadedScenes.Contains(sceneData.Name)) continue;
        var operation = SceneManager.LoadSceneAsync(sceneData.Name, LoadSceneMode.Additive);
        operationsGroup.Operations.Add(operation);
        OnSceneLoaded.Invoke(sceneData.Name);
      }
      // Wait until all AsyncOps in the group are done loading
      while (!operationsGroup.IsDone) {
        progress?.Report(operationsGroup.Progress);
        await Task.Delay(100);
      }
      // AsyncOps done
      Scene activeScene = SceneManager.GetSceneByName(_activeSceneGroup.FindSceneNameByType(SceneType.ActiveScene));
      if (activeScene.IsValid()) {
        SceneManager.SetActiveScene(activeScene);
      }
      OnSceneGroupLoaded.Invoke();
    }

    public async Task UnloadScenes() {
      var scenes = new List<string>();
      var activeScene = SceneManager.GetActiveScene().name;
      int sceneCount = SceneManager.sceneCount;

      for (int i = sceneCount - 1; i > 0; i--) {
        var sceneAt = SceneManager.GetSceneAt(i);
        if (!sceneAt.isLoaded) continue;

        var sceneName = sceneAt.name;
        // Performing exclusions
        if (sceneName.Equals(activeScene) || sceneName == "Bootstrapper") continue;
        scenes.Add(sceneName);
      }
      // Create an AsyncOperationGroup
      var operationsGroup = new AsyncOperationGroup(scenes.Count);
      foreach (var scene in scenes) {
        var operation = SceneManager.UnloadSceneAsync(scene);
        if (operation == null) continue;
        operationsGroup.Operations.Add(operation);
        OnSceneUnloaded.Invoke(scene);
      }
      // Wait until all AsyncOps in the group are done loading
      while (!operationsGroup.IsDone) {
        await Task.Delay(100);
      }
      //await Resources.UnloadUnusedAssets();
    }
  }
}