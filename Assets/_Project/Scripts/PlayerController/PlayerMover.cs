using KBCore.Refs;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace RealityWard.PlayerController {
  [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
  public class PlayerMover : MonoBehaviour {
    #region Fields
    [SerializeField] Rigidbody _rb;
    [SerializeField] bool _useTransformDirection = false;
    [SerializeField] float _rotationSpeed = 400f;
    [SerializeField] RaycastSensor _frontSensor;
    [SerializeField] RaycastSensor _backSensor;

    Transform _tr;
    bool _isGrounded;
    Vector3 _currentGroundAdjustmentVelocity; // Velocity to adjust player position to maintain ground contact
    int _currentLayer;

    [Header("Sensor Settings:")]
    [SerializeField] bool _isInDebugMode;
    #endregion

    void Awake() {
      Setup();
      RecalculateSensorLayerMask();
    }

    void LateUpdate() {
      if (_isInDebugMode) {
        _frontSensor.DrawDebug();
        _backSensor.DrawDebug();
      }
    }

    public void CheckTotalGroundAdjustment() {
      Vector3 frontAdjustment = CheckSensorGroundAdjustment(_frontSensor);
      Vector3 backAdjustment = CheckSensorGroundAdjustment(_backSensor);
      Vector3 oldDirection = _tr.position - _backSensor.WorldSpaceOrigin;
      Debug.DrawLine(_backSensor.WorldSpaceOrigin, _frontSensor.WorldSpaceOrigin, Color.red);
      Vector3 newDirection = (_frontSensor.WorldSpaceOrigin + frontAdjustment)
        - (_backSensor.WorldSpaceOrigin + backAdjustment);
      Debug.DrawLine((_backSensor.WorldSpaceOrigin + backAdjustment), (_frontSensor.WorldSpaceOrigin + frontAdjustment), Color.green);
      _currentGroundAdjustmentVelocity = Vector3.Project(oldDirection, newDirection);
      HandleXAxisRotation(newDirection);
    }

    public Vector3 CheckSensorGroundAdjustment(RaycastSensor sensor) {
      if (_currentLayer != gameObject.layer) {
        RecalculateSensorLayerMask();
      }

      sensor.Cast();

      _isGrounded = sensor.HasDetectedHit();
      if (!_isGrounded) return Vector3.zero;

      // Todo calc distance for tolerance
      float distance = sensor.GetDistance();

      return (_useTransformDirection ? _tr.up : Vector3.up) * (distance / Time.fixedDeltaTime);
    }

    private void HandleXAxisRotation(Vector3 adjustedDirection) {
      // Adjust the rotation to match the movement direction
      Quaternion targetRotation = Quaternion.LookRotation(adjustedDirection, _tr.right);
      transform.rotation = Quaternion.RotateTowards(transform.rotation,
        targetRotation, _rotationSpeed * Time.deltaTime);
    }

    public bool IsGrounded() => _isGrounded;
    public Vector3 GetFrontGroundNormal() => _frontSensor.GetNormal();
    public Vector3 GetBackGroundNormal() => _backSensor.GetNormal();

    // NOTE: Older versions of Unity use rb.velocity instead
    public void SetVelocity(Vector3 velocity) => _rb.linearVelocity = velocity + _currentGroundAdjustmentVelocity;

    void Setup() {
      _tr = transform;
      _backSensor.Setup(_tr, _useTransformDirection);
      _frontSensor.Setup(_tr, _useTransformDirection);
      _rb.freezeRotation = true;
      _rb.useGravity = false;
    }

    void RecalculateSensorLayerMask() {
      int objectLayer = gameObject.layer;
      int layerMask = Physics.AllLayers;

      for (int i = 0; i < 32; i++) {
        if (Physics.GetIgnoreLayerCollision(objectLayer, i)) {
          layerMask &= ~(1 << i);
        }
      }

      int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
      layerMask &= ~(1 << ignoreRaycastLayer);

      _frontSensor.Layermask(layerMask);
      _backSensor.Layermask(layerMask);
      _currentLayer = objectLayer;
    }

    void OnDrawGizmos() {
      Gizmos.color = Color.blue;
      Gizmos.DrawLine(transform.TransformPoint(_frontSensor.LocalSpaceOrigin),
        transform.TransformPoint(_frontSensor.LocalSpaceOrigin + Vector3.down * _frontSensor.GroundedDistance));
      Gizmos.DrawLine(transform.TransformPoint(_backSensor.LocalSpaceOrigin),
        transform.TransformPoint(_backSensor.LocalSpaceOrigin + Vector3.down * _backSensor.GroundedDistance));
      Gizmos.color = Color.red;
      Gizmos.DrawSphere(transform.TransformPoint(_frontSensor.LocalSpaceOrigin + Vector3.down * _frontSensor.MaxDistance),
        .005f);
      Gizmos.DrawSphere(transform.TransformPoint(_backSensor.LocalSpaceOrigin + Vector3.down * _backSensor.MaxDistance),
        .005f);
      Gizmos.color = Color.green;
      Gizmos.DrawSphere(transform.TransformPoint(_frontSensor.LocalSpaceOrigin + Vector3.down * _frontSensor.MinDistance),
        .005f);
      Gizmos.DrawSphere(transform.TransformPoint(_backSensor.LocalSpaceOrigin + Vector3.down * _backSensor.MinDistance),
        .005f);
    }
  }
}