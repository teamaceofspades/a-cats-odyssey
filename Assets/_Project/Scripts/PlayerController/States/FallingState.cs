using UnityEngine;

namespace PlayerController {
  public class FallingState : BaseState {
    public FallingState(PlayerController player, Animator animator) : base(player, animator) {
    }

    public override void OnEnter() {
      _player.OnFallStart();
    }
  }
}