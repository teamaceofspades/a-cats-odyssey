using UnityEngine;

namespace PlayerController.States {
  public class SlidingState : BaseState {
    public SlidingState(PlayerController player, Animator animator) : base(player, animator) {
    }

    public override void OnEnter() {
      _player.OnGroundContactLost();
    }
  }
}