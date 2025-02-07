using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
  public GameObject player;
  public CinemachineBrain brain;
  private Vector3 offset;

  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {
    offset = transform.position - player.transform.position;
  }

  // Update is called once per frame
  void LateUpdate()
  {
    //transform.position = player.transform.position + offset;
  }
}
