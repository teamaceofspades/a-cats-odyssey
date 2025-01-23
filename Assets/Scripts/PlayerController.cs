using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
  private Rigidbody rb;
  private float movementX;
  private float movementZ;

  public float speed = 0;
  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {
    rb = GetComponent<Rigidbody>();
  }

  private void FixedUpdate()
  {
    Vector3 movement = new Vector3(movementX, 0.0f, movementZ);
    rb.AddForce(movement * speed);
  }

  void OnMove(InputValue movementValue)
  {
    Vector2 movementVector = movementValue.Get<Vector2>();
    movementX = movementVector.x;
    movementZ = movementVector.y;
  }
}
