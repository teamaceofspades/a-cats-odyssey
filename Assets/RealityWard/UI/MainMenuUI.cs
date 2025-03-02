using UnityEngine;
using UnityEngine.UI;
using RealityWard.Utilities.Helpers;
using RealityWard.EventSystem;
using System;

namespace RealityWard.UI {
  public class MainMenuUI : MonoBehaviour {
    [SerializeField] IntEventChannel _sceneChangeChannel;
    [SerializeField] Button _playButton;
    [SerializeField] Button _quitButton;

    private void Awake() {
      _playButton.onClick.AddListener(LoadScene);
      _quitButton.onClick.AddListener(() => Application.Quit(0));
      Time.timeScale = 1f;
    }

    private void LoadScene() {
      if (_sceneChangeChannel != null) {
        _sceneChangeChannel.Invoke(1);
      }
    }
  }
}