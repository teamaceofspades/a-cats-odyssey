using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
  public GameObject PausePanel;
  public GameObject Player;

  // Update is called once per frame
  void Update()
  {

  }

  public void Pause()
  {
    PausePanel.SetActive(true);
    Time.timeScale = 0f;
    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;
  }

  public void OnUnpause()
  {
    PausePanel.SetActive(false);
    Time.timeScale = 1f;
    Cursor.visible = false;
    Cursor.lockState = CursorLockMode.Locked;
  }
}
