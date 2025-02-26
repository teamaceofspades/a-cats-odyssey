using UnityEngine;

namespace RealityWard.PlayerController {
  public class LocomotionState : BaseState {
    public LocomotionState(PlayerController player, Animator animator) : base(player, animator) {
    }

    public override void OnEnter() {
      Debug.Log("Loco Enter");
      _player.MoveSpeedMult = 1;
      //_animator.CrossFade(_LocomotionHash, _crossFadeDuration);
    }

    public override void FixedUpdate() {
      _player.HandleMovement();
    }
  }
}