using UnityEngine;

namespace PlayerController {
  public class RisingState : BaseState {
    public RisingState(PlayerController player, Animator animator) : base(player, animator) {
    }

    public override void OnEnter() {
      _player.OnGroundContactLost();
    }
  }
}