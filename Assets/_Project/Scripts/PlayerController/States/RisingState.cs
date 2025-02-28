using UnityEngine;

namespace RealityWard.PlayerController {
  public class RisingState : BaseState {
    public RisingState(PlayerController player, Animator animator) : base(player, animator) {
    }

    public override void OnEnter() {
      _player.OnGroundContactLost();
    }
  }
}