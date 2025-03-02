using KBCore.Refs;
using UnityEngine;

namespace PlayerController {
  public class CameraController : MonoBehaviour {
    #region Fields
    float _currentXAngle;
    float _currentYAngle;

    [Range(0f, 90f)] public float upperVerticalLimit = 35f;
    [Range(0f, 90f)] public float lowerVerticalLimit = 35f;

    public float cameraSpeed = 50f;
    public bool smoothCameraRotation;
    [Range(1f, 50f)] public float cameraSmoothingFactor = 25f;

    Transform _tr;
    Camera _cam;
    [SerializeField, Anywhere] InputReader _input;
    #endregion

    public Vector3 GetUpDirection() => _tr.up;
    public Vector3 GetFacingDirection() => _tr.forward;

    void Awake() {
      _tr = transform;
      _cam = GetComponentInChildren<Camera>();

      _currentXAngle = _tr.localRotation.eulerAngles.x;
      _currentYAngle = _tr.localRotation.eulerAngles.y;
    }

    void Update() {
      RotateCamera(_input.LookDirection.x, -_input.LookDirection.y);
    }

    void RotateCamera(float horizontalInput, float verticalInput) {
      if (smoothCameraRotation) {
        horizontalInput = Mathf.Lerp(0, horizontalInput, Time.deltaTime * cameraSmoothingFactor);
        verticalInput = Mathf.Lerp(0, verticalInput, Time.deltaTime * cameraSmoothingFactor);
      }

      _currentXAngle += verticalInput * cameraSpeed * Time.deltaTime;
      _currentYAngle += horizontalInput * cameraSpeed * Time.deltaTime;

      _currentXAngle = Mathf.Clamp(_currentXAngle, -upperVerticalLimit, lowerVerticalLimit);

      _tr.localRotation = Quaternion.Euler(_currentXAngle, _currentYAngle, 0);
    }
  }
}