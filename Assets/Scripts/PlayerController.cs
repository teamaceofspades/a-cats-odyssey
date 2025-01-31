using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
  public CharacterController characterController;
  public Transform playerCamera;
  public float speed = 0;
  public float turnTime = 0.1f;
  public GameObject spawnableItem;
  private float turnVelocity;

  public void Update()
  {
    float horizontal = Input.GetAxisRaw("Horizontal");
    float vertical = Input.GetAxisRaw("Vertical");
    Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

    if (direction.magnitude >= 0.1f)
    {
      // Update rotation
      float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + playerCamera.eulerAngles.y;
      float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, turnTime);
      transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);

      // Update movement
      Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
      characterController.Move(moveDirection.normalized * speed * Time.deltaTime);
    }
  }
}
