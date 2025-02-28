using KBCore.Refs;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace RealityWard.PlayerController {
  [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
  public class PlayerMover : MonoBehaviour {
    #region Fields
    [SerializeField] Rigidbody _rb;
    [SerializeField] PlayerController _player;
    [SerializeField] bool _useTransformDirection = false;
    [SerializeField] RaycastSensor _frontSensor;
    [SerializeField] RaycastSensor _backSensor;

    float _sensorOffset = 0f;
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

    void OnValidate() {
      Setup();
      _frontSensor.OnValidate();
      _backSensor.OnValidate();
    }

    float GetYInterceptForYZ(Vector3 a, Vector3 b) {
      return (a.y - ((a - b).y / (a - b).z) * a.z);
    }

    public void CheckGroundAdjustment() {
      Vector3 frontAdjustment, backAdjustment;
      NewMethod(out frontAdjustment, out backAdjustment);

      //Debug.Log(_backSensor.WorldSpaceOrigin);
      //Debug.Log(backAdjustment);
      //Debug.Log(_backSensor.WorldSpaceOrigin + backAdjustment);
      //Debug.Log("\n\n\n\n");
      Vector3 frontPoint = _frontSensor.LocalSpaceOrigin + frontAdjustment;
      Vector3 backPoint = _backSensor.LocalSpaceOrigin + backAdjustment;
      //Vector3 slide = CalcSlideVector();
      _currentGroundAdjustmentVelocity = new Vector3(0,
        (GetYInterceptForYZ(frontPoint, backPoint) - _sensorOffset) * 2, 0);
      HandleXAxisRotation(frontPoint - backPoint);
    }

    private void NewMethod(out Vector3 frontAdjustment, out Vector3 backAdjustment) {
      if (_currentLayer != gameObject.layer) {
        RecalculateSensorLayerMask();
      }
      _frontSensor.Cast();
      _backSensor.Cast();
      _isGrounded = _frontSensor.HasDetectedHit() || _backSensor.HasDetectedHit();

      frontAdjustment = CheckSensorGroundAdjustment(_frontSensor);
      backAdjustment = CheckSensorGroundAdjustment(_backSensor);
      Debug.DrawLine(_backSensor.WorldSpaceOrigin, _frontSensor.WorldSpaceOrigin, Color.red);
      Debug.DrawLine((_backSensor.WorldSpaceOrigin + backAdjustment),
        (_frontSensor.WorldSpaceOrigin + frontAdjustment), Color.green);
    }

    //private Vector3 CalcSlideVector() {

    //}

    Vector3 CheckSensorGroundAdjustment(RaycastSensor sensor) {
      if (!sensor.HasDetectedHit())
        return new(0, _player.Gravity, 0);
      //Debug.Log(sensor.GetDirectionVector());
      //Debug.Log(sensor.GetDistance());
      //Debug.Log(sensor.TargetDistance);
      //Debug.Log(Time.fixedDeltaTime);
      //Debug.Log(sensor.GetDirectionVector()
      //  * ((sensor.GetDistance() - sensor.TargetDistance)));
      //Debug.Log("\n\n\n\n");
      return sensor.GetDirectionVector()
        * ((sensor.GetDistance() - sensor.TargetDistance));
    }

    private void HandleXAxisRotation(Vector3 adjustedDirection) {
      // Adjust the rotation to match the movement direction
      Quaternion targetRotation = Quaternion.LookRotation(adjustedDirection, Vector3.up);
      Debug.DrawRay(_backSensor.WorldSpaceOrigin, targetRotation * _tr.forward);
      transform.rotation = Quaternion.Slerp(transform.rotation,
        targetRotation, .85f);
    }

    public bool IsGrounded() => _isGrounded;
    public Vector3 GetGroundNormal() => (_frontSensor.GetNormal() + _backSensor.GetNormal()) / 2;
    public Vector3 GetFrontGroundNormal() => _frontSensor.GetNormal();
    public Vector3 GetBackGroundNormal() => _backSensor.GetNormal();

    // NOTE: Older versions of Unity use rb.velocity instead
    public void SetVelocity(Vector3 velocity) => _rb.linearVelocity = velocity + _currentGroundAdjustmentVelocity;

    void Setup() {
      _rb = gameObject.GetComponent<Rigidbody>();
      _rb.freezeRotation = true;
      _rb.useGravity = false;
      _player = gameObject.GetComponent<PlayerController>();
      _tr = transform;
      _backSensor.Setup(_tr, _useTransformDirection);
      _frontSensor.Setup(_tr, _useTransformDirection);
      _frontSensor.SetCastDirection(RaycastSensor.CastDirection.Down);
      _backSensor.SetCastDirection(RaycastSensor.CastDirection.Down);
      _sensorOffset = GetYInterceptForYZ(_frontSensor.LocalSpaceOrigin, _backSensor.LocalSpaceOrigin);
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
      Gizmos.DrawLine((_frontSensor.WorldSpaceOrigin),
        (_frontSensor.WorldSpaceOrigin + Vector3.down * _frontSensor.GroundedDistance));
      Gizmos.DrawLine((_backSensor.WorldSpaceOrigin),
        (_backSensor.WorldSpaceOrigin + Vector3.down * _backSensor.GroundedDistance));
      Gizmos.color = Color.red;
      Gizmos.DrawSphere((_frontSensor.WorldSpaceOrigin + Vector3.down * _frontSensor.TargetDistance),
        .005f);
      Gizmos.DrawSphere((_backSensor.WorldSpaceOrigin + Vector3.down * _backSensor.TargetDistance),
        .005f);
      Gizmos.color = Color.green;
      Gizmos.DrawSphere((_frontSensor.WorldSpaceOrigin + Vector3.down * _frontSensor.MinDistance),
        .005f);
      Gizmos.DrawSphere((_backSensor.WorldSpaceOrigin + Vector3.down * _backSensor.MinDistance),
        .005f);

      //Gizmos.color = Color.blue;
      //Gizmos.DrawLine(transform.TransformPoint(_frontSensor.LocalSpaceOrigin),
      //  transform.TransformPoint(_frontSensor.LocalSpaceOrigin + Vector3.down * _frontSensor.GroundedDistance));
      //Gizmos.DrawLine(transform.TransformPoint(_backSensor.LocalSpaceOrigin),
      //  transform.TransformPoint(_backSensor.LocalSpaceOrigin + Vector3.down * _backSensor.GroundedDistance));
      //Gizmos.color = Color.red;
      //Gizmos.DrawSphere(transform.TransformPoint(_frontSensor.LocalSpaceOrigin + Vector3.down * _frontSensor.TargetDistance),
      //  .005f);
      //Gizmos.DrawSphere(transform.TransformPoint(_backSensor.LocalSpaceOrigin + Vector3.down * _backSensor.TargetDistance),
      //  .005f);
      //Gizmos.color = Color.green;
      //Gizmos.DrawSphere(transform.TransformPoint(_frontSensor.LocalSpaceOrigin + Vector3.down * _frontSensor.MinDistance),
      //  .005f);
      //Gizmos.DrawSphere(transform.TransformPoint(_backSensor.LocalSpaceOrigin + Vector3.down * _backSensor.MinDistance),
      //  .005f);
      Setup();
      Vector3 frontAdjustment, backAdjustment;
      //NewMethod(out frontAdjustment, out backAdjustment);
    }
  }
}