using UnityEngine;

namespace PlayerController {
  public class JumpingState : BaseState {
    public JumpingState(PlayerController player, Animator animator) : base(player, animator) {
    }

    public override void OnEnter() {
      _player.OnGroundContactLost();
      _player.OnJumpStart();
    }
  }
}