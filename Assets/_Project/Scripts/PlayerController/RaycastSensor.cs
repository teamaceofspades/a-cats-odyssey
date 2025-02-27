using System;
using UnityEngine;

namespace RealityWard.PlayerController {
  [Serializable]
  public class RaycastSensor {
    public float MaxDistance = 2f;
    public float MinDistance = .1f;
    public float GroundedDistance = 2.05f;
    [SerializeField] Vector3 _localSpaceOrigin = Vector3.zero;

    public Vector3 LocalSpaceOrigin { get { return _localSpaceOrigin; } }
    public Vector3 WorldSpaceOrigin { get; private set; }

    Transform _tr;
    bool _useTransformDirection;
    LayerMask _layermask = 255;
    RaycastHit _hitInfo;

    public enum CastDirection { Forward, Right, Up, Backward, Left, Down }
    CastDirection _castDirection;

    public void Layermask(LayerMask value) => _layermask = value;

    public void Setup(Transform playerTransform, bool useTransformDirection) {
      _tr = playerTransform;
      _useTransformDirection = useTransformDirection;
    }

    public void Cast() {
      WorldSpaceOrigin = _tr.TransformPoint(_localSpaceOrigin);
      Vector3 worldDirection = GetDirectionVector();

      Physics.Raycast(WorldSpaceOrigin, worldDirection, out _hitInfo, GroundedDistance, _layermask, QueryTriggerInteraction.Ignore);
    }

    public bool HasDetectedHit() => _hitInfo.collider != null;
    public float GetDistance() => _hitInfo.distance;
    public Vector3 GetNormal() => _hitInfo.normal;
    public Vector3 GetPosition() => _hitInfo.point;
    public Collider GetCollider() => _hitInfo.collider;
    public Transform GetTransform() => _hitInfo.transform;

    public void SetCastDirection(CastDirection direction) => _castDirection = direction;
    public Vector3 GetDirectionVector() => _useTransformDirection ? GetCastDirection() : GetWorldCastDirection();
    public void SetCastOrigin(Vector3 pos) => _localSpaceOrigin = _tr.InverseTransformPoint(pos);

    Vector3 GetCastDirection() {
      return _castDirection switch {
        CastDirection.Forward => _tr.forward,
        CastDirection.Right => _tr.right,
        CastDirection.Up => _tr.up,
        CastDirection.Backward => -_tr.forward,
        CastDirection.Left => -_tr.right,
        CastDirection.Down => -_tr.up,
        _ => Vector3.one
      };
    }

    Vector3 GetWorldCastDirection() {
      return _castDirection switch {
        CastDirection.Forward => Vector3.forward,
        CastDirection.Right => Vector3.right,
        CastDirection.Up => Vector3.up,
        CastDirection.Backward => Vector3.back,
        CastDirection.Left => Vector3.left,
        CastDirection.Down => Vector3.down,
        _ => Vector3.one
      };
    }

    public void DrawDebug() {
      if (!HasDetectedHit()) return;

      Debug.DrawRay(_hitInfo.point, _hitInfo.normal, Color.red, Time.deltaTime);
      float markerSize = 0.2f;
      Debug.DrawLine(_hitInfo.point + Vector3.up * markerSize, _hitInfo.point - Vector3.up * markerSize, Color.green, Time.deltaTime);
      Debug.DrawLine(_hitInfo.point + Vector3.right * markerSize, _hitInfo.point - Vector3.right * markerSize, Color.green, Time.deltaTime);
      Debug.DrawLine(_hitInfo.point + Vector3.forward * markerSize, _hitInfo.point - Vector3.forward * markerSize, Color.green, Time.deltaTime);
    }
  }
}