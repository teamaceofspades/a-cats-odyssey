using UnityEngine;

namespace RealityWard.PlayerController {
  public class AttackState : BaseState {
    public AttackState(PlayerController player, Animator animator) : base(player, animator) {
    }

    public override void OnEnter() {
      //_animator.CrossFade(_AttackHash, _crossFadeDuration);
      _player.Attack();
    }

    public override void FixedUpdate() {
      _player.HandleMovement();
    }
  }
}