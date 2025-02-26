using UnityEngine;
using RealityWard.EventSystem;
using RealityWard.PlayerController;

namespace RealityWard.AI {
  public class PlayerDetector : MonoBehaviour {
    /// <summary> Cone in front of enemy </summary>
    [SerializeField] float _detectionAngle = 60f;
    /// <summary> Cone length </summary>
    [SerializeField] float _detectionRadius = 10f;
    /// <summary> Small circle around enemy </summary>
    [SerializeField] float _innerDetectionRadius = 5f;
    /// <summary> Time between detections </summary>
    [SerializeField] float _detectionCooldown = 1f;
    /// <summary> Reach distance to hit player </summary>
    [SerializeField] float _attackRange = 2f;

    public Transform Player { get; private set; }
    public HealthEventModule PlayerHealth { get; private set; }
    CountdownTimer _detectionTimer;

    IDetectionStrategy _detectionStrategy;

    private void Awake() {
      Player = GameObject.FindGameObjectWithTag("Player").transform;
      PlayerHealth = Player.GetComponent<HealthEventModule>();
    }

    private void Start() {
      _detectionTimer = new CountdownTimer(_detectionCooldown);
      _detectionStrategy =
        new ConeDetectionStrategy(_detectionAngle,
        _detectionRadius, _innerDetectionRadius);
    }

    void Update() => _detectionTimer.Tick(Time.deltaTime);

    public bool CanDetectPlayer() {
      return _detectionTimer.IsRunning ||
        _detectionStrategy.Execute(Player, transform, _detectionTimer);
    }

    public bool CanAttackPlayer() {
      var directionToPlayer = Player.position - transform.position;
      return directionToPlayer.magnitude <= _attackRange;
    }

    public void SetDetectionStrategy(IDetectionStrategy detectionStrategy) => _detectionStrategy = detectionStrategy;

    void OnDrawGizmos() {
      Gizmos.color = Color.red;

      // Draw a spheres for the radii
      Gizmos.DrawWireSphere(transform.position, _detectionRadius);
      Gizmos.DrawWireSphere(transform.position, _innerDetectionRadius);

      // Calculate our cone directions
      Vector3 forwardConeDirection = Quaternion.Euler(0, _detectionAngle / 2, 0) * transform.forward * _detectionRadius;
      Vector3 backwardConeDirection = Quaternion.Euler(0, -_detectionAngle / 2, 0) * transform.forward * _detectionRadius;

      // Draw lines to represent the cone
      Gizmos.DrawLine(transform.position, transform.position + forwardConeDirection);
      Gizmos.DrawLine(transform.position, transform.position + backwardConeDirection);
    }
  }
}