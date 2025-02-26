using RealityWard.EventSystem;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace RealityWard.Utilities.SceneManagement {
  public class SceneLoader : MonoBehaviour {
    [SerializeField] Image _loadingBar;
    [SerializeField] float _fillSpeed = .5f;
    [SerializeField] Canvas _loadingCanvas;
    [SerializeField] Camera _loadingCamera;
    [SerializeField] BoolEventChannel _cameraEnableChannel;
    [SerializeField] SceneGroup[] _sceneGroups;

    float _targetProgress;
    bool _isLoading;

    public readonly SceneGroupManager Manager = new();

    async void Start() {
      await LoadSceneGroup(0);
    }

    private void Update() {
      if (!_isLoading) return;

      float currentFillAmount = _loadingBar.fillAmount;
      float progressDifference = Mathf.Abs(currentFillAmount - _targetProgress);

      float dynamicFillSpeed = progressDifference * _fillSpeed;

      _loadingBar.fillAmount = Mathf.Lerp(currentFillAmount, _targetProgress, Time.deltaTime * dynamicFillSpeed);
    }

    public async void LoadSceneGroupByName(string name) {
      for (int i = 0; i < _sceneGroups.Length; i++) {
        if (_sceneGroups[i].Name == name) {
          await LoadSceneGroup(i);
          return;
        }
      }
      Debug.LogError($"There is no scene group called {name}");
    }

    public async void LoadSceneGroupByIndex(int index) {
      await LoadSceneGroup(index);
    }

    public async Task LoadSceneGroup(int index) {
      _loadingBar.fillAmount = 0f;
      _targetProgress = 1f;

      if (index < 0 || index >= _sceneGroups.Length) {
        Debug.LogError($"Invalid scene group index: {index}");
        return;
      }

      LoadingProgress progress = new();
      progress.Progressed += target => _targetProgress = Mathf.Max(target, _targetProgress);

      EnableCamerasInScene(false);
      EnableLoadingCanvas();
      await Manager.LoadScenes(_sceneGroups[index], progress);
      EnableLoadingCanvas(false);
      EnableCamerasInScene();
    }

    void EnableLoadingCanvas(bool enable = true) {
      _isLoading = enable;
      _loadingCanvas.gameObject.SetActive(enable);
      _loadingCamera.gameObject.SetActive(enable);
    }

    void EnableCamerasInScene(bool enable = true) {
      _cameraEnableChannel?.Invoke(enable);
    }
  }
}