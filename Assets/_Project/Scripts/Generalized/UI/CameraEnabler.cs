using RealityWard.EventSystem;
using UnityEngine;

public class CameraEnabler : MonoBehaviour {
  private void Awake() {
    EnableChildCamera(false);
  }
  public void EnableChildCamera(bool enable) {
    try {
      gameObject.GetComponentInChildren<Camera>(true).gameObject.SetActive(enable);
    }
    catch (System.Exception) {
      Debug.LogError($"Could not find camera in children of {gameObject.name}.");
    }
  }
}
