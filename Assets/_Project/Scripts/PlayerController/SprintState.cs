using UnityEngine;

namespace RealityWard.PlayerController {
  public class SprintState : BaseState {
    public SprintState(PlayerController player, Animator animator) : base(player, animator) {
    }

    public override void OnEnter() {
      Debug.Log("Sprint Enter");
      _player.MoveSpeedMult *= _player.SprintSpeedMult;
      //_animator.CrossFade(_SprintHash, _crossFadeDuration);
    }

    public override void FixedUpdate() {
      _player.HandleMovement();
    }
  }
}