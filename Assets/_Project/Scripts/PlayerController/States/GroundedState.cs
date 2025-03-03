using UnityEngine;

namespace PlayerController.States {
  public class GroundedState : BaseState {
    public GroundedState(PlayerController player, Animator animator) : base(player, animator) {
    }

    public override void OnEnter() {
      _player.OnGroundContactRegained();
    }
  }
}