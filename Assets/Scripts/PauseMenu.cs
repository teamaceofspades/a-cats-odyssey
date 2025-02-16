using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
  public GameObject PausePanel;
  public GameObject Player;

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

  public void ExitBtn()
  {
    SceneManager.LoadScene(0, LoadSceneMode.Single);
  }
}
