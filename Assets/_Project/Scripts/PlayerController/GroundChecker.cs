using UnityEngine;

namespace RealityWard.PlayerController {
  public class GroundChecker : MonoBehaviour {
    [SerializeField] float _groundDistance = .08f;
    [SerializeField] LayerMask _groundLayers;
    public bool IsGrounded { get; private set; }

    private void Update() {
      IsGrounded = Physics.SphereCast(transform.position, _groundDistance, Vector3.down,
        out _, _groundDistance, _groundLayers);
    }
  }
}