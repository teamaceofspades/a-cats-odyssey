using UnityEngine;
using RealityWard.StateMachineSystem;

namespace RealityWard.PlayerController {
  public abstract class BaseState : IState {
    protected readonly PlayerController _player;
    protected readonly Animator _animator;
    protected const float _crossFadeDuration = 0.1f;

    //protected static readonly int _LocomotionHash = Animator.StringToHash("Locomotion");
    //protected static readonly int _JumpHash = Animator.StringToHash("Jump");
    //protected static readonly int _SprintHash = Animator.StringToHash("Sprint");
    //protected static readonly int _AttackHash = Animator.StringToHash("Attack");

    protected BaseState(PlayerController player, Animator animator) {
      _player = player;
      _animator = animator;
    }

    public virtual void Update() {
      //noop
    }

    public virtual void FixedUpdate() {
      //noop
    }

    public virtual void OnEnter() {
      //noop
    }

    public virtual void OnExit() {
    }
  }
}