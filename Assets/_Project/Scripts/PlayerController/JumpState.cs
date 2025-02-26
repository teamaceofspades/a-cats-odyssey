using UnityEngine;

namespace RealityWard.PlayerController {
  public class JumpState : BaseState {
    public JumpState(PlayerController player, Animator animator) : base(player, animator) {
    }

    public override void OnEnter() {
      Debug.Log("Jump Enter");
      //_animator.CrossFade(_JumpHash, _crossFadeDuration);
    }

    public override void FixedUpdate() {
      _player.HandleJump();
      _player.HandleMovement();
    }
  }
}