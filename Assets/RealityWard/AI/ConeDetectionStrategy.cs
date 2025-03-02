using UnityEngine;
using RealityWard.EventSystem;

namespace RealityWard.AI {
  public class ConeDetectionStrategy : IDetectionStrategy {
    readonly float _DetectionAngle;
    readonly float _DetectionRadius;
    readonly float _InnerDetectionRadius;

    public ConeDetectionStrategy(float detectionAngle,
      float detectionRadius, float innerDetectionRadius) {
      _DetectionAngle = detectionAngle;
      _DetectionRadius = detectionRadius;
      _InnerDetectionRadius = innerDetectionRadius;
    }

    public bool Execute(Transform player, Transform detector, CountdownTimer timer) {
      if (timer.IsRunning) return false;

      var directionToPlayer = player.position - detector.position;
      var angleToPlayer = Vector3.Angle(directionToPlayer, detector.forward);

      // If the player is not within the detection angle + outer radius
      // (aka the cone in front of the enemy)
      // or is within the inner radius, return false
      if ((!(angleToPlayer < _DetectionAngle / 2f)
        || !(directionToPlayer.magnitude < _DetectionRadius))
        && !(directionToPlayer.magnitude < _InnerDetectionRadius))
        return false;

      timer.Start();
      return true;
    }
  }
}