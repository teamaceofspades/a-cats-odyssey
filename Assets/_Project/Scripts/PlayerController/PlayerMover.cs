using RealityWard.Utilities.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace PlayerController {
  [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
  public class PlayerMover : MonoBehaviour {
    #region Fields
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private PlayerController _player;
    [SerializeField] private bool _isInDebugMode;
    /// <summary>
    /// The maximum degrees of rotation possible per CheckGroundAdjustment().<br />
    /// Validation: <c>value &lt; 180f &amp;&amp; value &gt;= 0f</c>
    /// </summary>
    [SerializeField] private float _maxAdjAngle = 90f;

    private float _sensorOffset = 0f;
    private Transform _tr;
    private bool _isGroundedPossible;
    private Vector3 _currentGroundAdjustmentVelocity; // Velocity to adjust player position to maintain ground contact
    private int _currentLayer;
    private int _layerMask;

    [Header("Sensor Settings:")]
    [SerializeField] private bool _useTransformDirection = false;
    [SerializeField] private RaycastSensor _frontSensor;
    [SerializeField] private RaycastSensor _backSensor;

    public float MaxAdjAngle {
      get { return _maxAdjAngle; }
      set {
        if (value >= 180f) _maxAdjAngle = 179.99f;
        else if (value < 0f) _maxAdjAngle = 0f;
        else _maxAdjAngle = value;
      }
    }
    #endregion

    private void Awake() {
      BaseSetup();
      RecalculateSensorLayerMask();
    }

    private void LateUpdate() {
      if (_isInDebugMode) {
        _frontSensor.DrawDebug();
        _backSensor.DrawDebug();
      }
    }

    private void OnValidate() {
      BaseSetup();
      _frontSensor.OnValidate();
      _backSensor.OnValidate();
    }

    /// <summary>
    /// Gets the y-intercept from a line in a YZ plane.
    /// This only works if <c>a.x - b.x = 0</c>.
    /// If the line is parallel to the Y axis, returns 0.
    /// </summary>
    /// <param name="a">Point A.</param>
    /// <param name="b">Point B.</param>
    /// <returns>y-intercept</returns>
    private float GetYInterceptForYZ(Vector3 a, Vector3 b) {
      Vector3 v = a - b;
      if (v.z == 0)
        return 0;
      return a.y - (v.y / v.z) * a.z;
    }

    public enum FallState {
      Grounded,
      FrontFall,
      BackFall,
      FreeFall
    }
    private FallState _fallState = FallState.Grounded;
    public FallState SensorFallState { get { return _fallState; } }
    public void CheckGroundAdjustment() {
      if (_currentLayer != gameObject.layer) {
        RecalculateSensorLayerMask();
      }
      _frontSensor.Cast();
      _backSensor.Cast();
      UpdateFallState();

      Vector3 frontAdjustment = CheckSensorGroundAdjustment(_frontSensor);
      Vector3 backAdjustment = CheckSensorGroundAdjustment(_backSensor);

      if (_isInDebugMode) {
        Debug.DrawLine(_backSensor.WorldSpaceOrigin, _frontSensor.WorldSpaceOrigin, Color.red);
        Debug.DrawLine(_backSensor.WorldSpaceOrigin + backAdjustment,
          _frontSensor.WorldSpaceOrigin + frontAdjustment, Color.green);
      }

      Vector3 adjustedDirection = _frontSensor.WorldSpaceOrigin + frontAdjustment - (_backSensor.WorldSpaceOrigin + backAdjustment);
      // initial angle
      float angleRatio = Vector3.Angle(_tr.forward, adjustedDirection);
      // check adjustedDirection rotation against _maxAdjAngle
      if (angleRatio > _maxAdjAngle) {
        // set to actual ratio
        angleRatio = _maxAdjAngle / angleRatio;
        // get new direction at _maxAdjAngle
        adjustedDirection = Vector3.Slerp(_tr.forward, adjustedDirection, angleRatio);
      }
      else {
        // if less than _maxAdjAngle set to 1 for no scalar effect
        angleRatio = 1f;
      }

      _currentGroundAdjustmentVelocity = new Vector3(0,
          GetYInterceptForYZ(_frontSensor.LocalSpaceOrigin + frontAdjustment * angleRatio, _backSensor.LocalSpaceOrigin + backAdjustment * angleRatio) - _sensorOffset, 0);
      HandleXAxisRotation(adjustedDirection);

      Vector3 horzontalAdjustment = Vector3.zero;
      if (_fallState == FallState.FrontFall) {
        horzontalAdjustment += CalcHorizontalAdjustment(_frontSensor, _backSensor);
      }
      else if (_fallState == FallState.BackFall) {
        horzontalAdjustment += CalcHorizontalAdjustment(_backSensor, _frontSensor);
      }

      // get height adjustment based on where the adjustedDirection intercepts the local y axis
      _currentGroundAdjustmentVelocity *= _player.MovementSpeed * 10;
      _currentGroundAdjustmentVelocity += horzontalAdjustment;
    }

    private Vector3 CalcHorizontalAdjustment(RaycastSensor fallingSensor, RaycastSensor groundedSensor) {
      Vector3 result = Vector3.zero;
      Vector3 startPoint = groundedSensor.WorldSpaceOrigin + _currentGroundAdjustmentVelocity + groundedSensor.GetDirectionVector() * groundedSensor.MinDistance;
      Vector3 endPoint = fallingSensor.WorldSpaceOrigin + _currentGroundAdjustmentVelocity + fallingSensor.GetDirectionVector() * fallingSensor.TargetDistance;
      RaycastHit hitInfo;
      float distance = Vector3.Distance(startPoint, endPoint);
      Vector3 ray = endPoint - startPoint;
      if (Physics.Raycast(startPoint, ray, out hitInfo, distance, _layerMask, QueryTriggerInteraction.Ignore)) {
        ray *= hitInfo.distance / distance;
        result += Vector3.ProjectOnPlane(ray, groundedSensor.GetDirectionVector());
      }
      return result * .9f;
    }

    private void UpdateFallState() {
      if (_frontSensor.HasDetectedHit() && _backSensor.HasDetectedHit()) {
        if (_frontSensor.HitDistance() <= _frontSensor.MaxDistance) {
          if (_backSensor.HitDistance() <= _backSensor.MaxDistance) {
            _fallState = FallState.Grounded;
          }
          else if (_fallState != FallState.FreeFall) {
            _fallState = FallState.BackFall;
          }
        }
        else if (_backSensor.HitDistance() <= _backSensor.MaxDistance && _fallState != FallState.FreeFall) {
          _fallState = FallState.FrontFall;
        }
        else {
          _fallState = FallState.FreeFall;
        }
      }
      else {
        _fallState = FallState.FreeFall;
      }
    }

    //private Vector3 CalcSlideVector() {

    //}

    private Vector3 CheckSensorGroundAdjustment(RaycastSensor sensor) {
      if (!sensor.HasDetectedHit())
        return new(0, _player.Gravity * Time.fixedDeltaTime, 0);
      return (sensor.HitDistance() - sensor.TargetDistance)
        * sensor.GetDirectionVector();
    }

    private void HandleXAxisRotation(Vector3 adjustedDirection) {
      // Adjust the rotation to match the movement direction
      Quaternion targetRotation
        = Quaternion.LookRotation(adjustedDirection, Vector3.up);
      _rb.MoveRotation(Quaternion.Slerp(_rb.rotation,
        targetRotation, .99f));

      //float angleDifference = VectorMath.GetAngle(_tr.forward, adjustedDirection.normalized, Vector3.up);

      //float step = Mathf.Sign(angleDifference) *
      //             Mathf.InverseLerp(0f, _fallOffAngle, Mathf.Abs(angleDifference)) *
      //             Time.deltaTime * TurnSpeed;
    }

    public bool IsGrounded() => _isGroundedPossible;
    public Vector3 GetAverageGroundNormal() => (_frontSensor.GetNormal() + _backSensor.GetNormal()) / 2;
    public Vector3 GetFrontGroundNormal() => _frontSensor.GetNormal();
    public Vector3 GetBackGroundNormal() => _backSensor.GetNormal();

    /// <summary>
    /// Set the linearVelocity of the rigidbody component plus the _currentGroundAdjustmentVelocity for smoothing elevation changes.
    /// </summary>
    /// <param name="velocity">New velocity no adjustments.</param>
    public void SetVelocity(Vector3 velocity) => _rb.linearVelocity = velocity + _currentGroundAdjustmentVelocity;

    /// <summary>
    /// Finish setup for PlayerMover.
    /// </summary>
    private void BaseSetup() {
      _rb = gameObject.GetComponent<Rigidbody>();
      _rb.freezeRotation = true;
      _rb.useGravity = false;
      _player = gameObject.GetComponent<PlayerController>();
      _tr = transform;
      SensorSetup();
    }

    /// <summary>
    /// Initialize RaycastSensors.
    /// </summary>
    private void SensorSetup() {
      _backSensor.Setup(_tr, _useTransformDirection);
      _frontSensor.Setup(_tr, _useTransformDirection);
      // set direction to check
      _frontSensor.SetCastDirection(RaycastSensor.CastDirection.Down);
      _backSensor.SetCastDirection(RaycastSensor.CastDirection.Down);
      // get offset from player transform origin
      _sensorOffset = GetYInterceptForYZ(_frontSensor.LocalSpaceOrigin, _backSensor.LocalSpaceOrigin);
      // get cast length
      _frontSensor.CastDistance = _frontSensor.TargetDistance + _backSensor.TargetDistance + Vector3.Distance(_frontSensor.LocalSpaceOrigin, _backSensor.LocalSpaceOrigin) * .6f - _backSensor.MinDistance;
      _backSensor.CastDistance = _backSensor.TargetDistance + _frontSensor.TargetDistance + Vector3.Distance(_backSensor.LocalSpaceOrigin, _frontSensor.LocalSpaceOrigin) * .6f - _frontSensor.MinDistance;
    }

    private void RecalculateSensorLayerMask() {
      int objectLayer = gameObject.layer;
      _layerMask = Physics.AllLayers;

      for (int i = 0; i < 32; i++) {
        if (Physics.GetIgnoreLayerCollision(objectLayer, i)) {
          _layerMask &= ~(1 << i);
        }
      }

      int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
      _layerMask &= ~(1 << ignoreRaycastLayer);

      _frontSensor.Layermask(_layerMask);
      _backSensor.Layermask(_layerMask);
      _currentLayer = objectLayer;
    }

    private void OnDrawGizmos() {
      if (_isInDebugMode) {
        // Show Cast Distance
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.TransformPoint(_frontSensor.LocalSpaceOrigin),
          transform.TransformPoint(_frontSensor.LocalSpaceOrigin) + Vector3.down * _frontSensor.CastDistance);
        Gizmos.DrawLine(transform.TransformPoint(_backSensor.LocalSpaceOrigin),
          transform.TransformPoint(_backSensor.LocalSpaceOrigin) + Vector3.down * _backSensor.CastDistance);
        // Show Max Distance
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.TransformPoint(_frontSensor.LocalSpaceOrigin) + Vector3.down * _frontSensor.MaxDistance,
          .005f);
        Gizmos.DrawSphere(transform.TransformPoint(_backSensor.LocalSpaceOrigin) + Vector3.down * _backSensor.MaxDistance,
          .005f);
        // Show Target Distance
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(_frontSensor.LocalSpaceOrigin) + Vector3.down * _frontSensor.TargetDistance,
          .005f);
        Gizmos.DrawSphere(transform.TransformPoint(_backSensor.LocalSpaceOrigin) + Vector3.down * _backSensor.TargetDistance,
          .005f);
        // Show Minimum Distance
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(_frontSensor.LocalSpaceOrigin) + Vector3.down * _frontSensor.MinDistance,
          .005f);
        Gizmos.DrawSphere(transform.TransformPoint(_backSensor.LocalSpaceOrigin) + Vector3.down * _backSensor.MinDistance,
          .005f);
      }
    }
  }
}