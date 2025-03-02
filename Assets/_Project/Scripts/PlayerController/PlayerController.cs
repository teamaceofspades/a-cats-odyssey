using RealityWard.EventSystem;
using RealityWard.StateMachineSystem;
using RealityWard.Utilities.Helpers;
using System;
using UnityEngine;

namespace PlayerController {
  [RequireComponent(typeof(PlayerMover))]
  public class PlayerController : MonoBehaviour {
    #region Fields
    [SerializeField] InputReader _input;

    Transform _tr;
    Animator _animator;
    PlayerMover _mover;
    CeilingDetector _ceilingDetector;

    bool JumpKeyIsPressed => _input.InputActions.Player.Jump.IsPressed();
    bool JumpKeyWasPressed => _input.InputActions.Player.Jump.WasPressedThisFrame();
    bool JumpKeyWasLetGo => _input.InputActions.Player.Jump.WasReleasedThisFrame();
    bool _jumpInputIsLocked;

    public float MovementSpeed = 7f;
    public float TurnSpeed = 50f;
    public float AirControlRate = 2f;
    public float JumpSpeed = 10f;
    public float JumpDuration = 0.2f;
    public float AirFriction = 0.5f;
    public float GroundFriction = 100f;
    public float Gravity = -30f;
    public float SlideGravity = -5f;
    public float SlopeLimit = 30f;
    public bool UseLocalMomentum;
    public float CurrentYRotation;
    const float _fallOffAngle = 90f;

    StateMachine _stateMachine;
    CountdownTimer _jumpTimer;

    [SerializeField] Transform _cameraTransform;

    Vector3 _momentum, _savedVelocity, _savedMovementVelocity;

    public event Action<Vector3> OnJump = delegate { };
    public event Action<Vector3> OnLand = delegate { };
    #endregion

    bool IsGrounded() => _stateMachine.CurrentState is GroundedState or SlidingState;
    public Vector3 GetVelocity() => _savedVelocity;
    public Vector3 GetMomentum() => UseLocalMomentum ? _tr.localToWorldMatrix * _momentum : _momentum;
    public Vector3 GetMovementVelocity() => _savedMovementVelocity;

    void Awake() {
      _tr = transform;
      _mover = GetComponent<PlayerMover>();
      _animator = GetComponent<Animator>();
      _ceilingDetector = GetComponent<CeilingDetector>();
      CurrentYRotation = _tr.localEulerAngles.y;

      _jumpTimer = new CountdownTimer(JumpDuration);
      SetupStateMachine();
    }

    void Start() {
      _input.EnablePlayerActions();
      _input.Jump += HandleJumpKeyInput;
    }

    void HandleJumpKeyInput(bool isButtonPressed) {
      if (JumpKeyIsPressed && !isButtonPressed) {
        _jumpInputIsLocked = false;
      }
    }

    void SetupStateMachine() {
      _stateMachine = new StateMachine();

      var grounded = new GroundedState(this, _animator);
      var falling = new FallingState(this, _animator);
      var sliding = new SlidingState(this, _animator);
      var rising = new RisingState(this, _animator);
      var jumping = new JumpingState(this, _animator);

      At(grounded, rising, () => IsRising());
      At(grounded, sliding, () => _mover.IsGrounded() && IsGroundTooSteep());
      At(grounded, falling, () => !_mover.IsGrounded());
      At(grounded, jumping, () => (JumpKeyIsPressed || JumpKeyWasPressed) && !_jumpInputIsLocked);

      At(falling, rising, () => IsRising());
      At(falling, grounded, () => _mover.IsGrounded() && !IsGroundTooSteep());
      At(falling, sliding, () => _mover.IsGrounded() && IsGroundTooSteep());

      At(sliding, rising, () => IsRising());
      At(sliding, falling, () => !_mover.IsGrounded());
      At(sliding, grounded, () => _mover.IsGrounded() && !IsGroundTooSteep());

      At(rising, grounded, () => _mover.IsGrounded() && !IsGroundTooSteep());
      At(rising, sliding, () => _mover.IsGrounded() && IsGroundTooSteep());
      At(rising, falling, () => IsFalling());
      At(rising, falling, () => _ceilingDetector != null && _ceilingDetector.HitCeiling());

      At(jumping, rising, () => _jumpTimer.IsFinished || JumpKeyWasLetGo);
      At(jumping, falling, () => _ceilingDetector != null && _ceilingDetector.HitCeiling());

      _stateMachine.SetState(falling);
    }

    void At(IState from, IState to, Func<bool> condition) => _stateMachine.AddTransition(from, to, condition);
    void Any<T>(IState to, Func<bool> condition) => _stateMachine.AddAnyTransition(to, condition);

    bool IsRising() => VectorMath.GetDotProduct(GetMomentum(), _tr.up) > 0f;
    bool IsFalling() => VectorMath.GetDotProduct(GetMomentum(), _tr.up) < 0f;
    bool IsGroundTooSteep() => !_mover.IsGrounded() || Vector3.Angle(_mover.GetGroundNormal(), _tr.up) > SlopeLimit;

    //void Update() => _stateMachine.Update();

    void FixedUpdate() {
      //_stateMachine.FixedUpdate();
      _mover.CheckGroundAdjustment();
      //HandleMomentum();
      //Vector3 velocity = _stateMachine.CurrentState is GroundedState ? CalculateMovementVelocity() : Vector3.zero;
      //velocity += UseLocalMomentum ? _tr.localToWorldMatrix * _momentum : _momentum;

      //_mover.SetVelocity(velocity);
      _mover.SetVelocity(Vector3.zero);

      //_savedVelocity = velocity;
      //_savedMovementVelocity = CalculateMovementVelocity();

      //if (_ceilingDetector != null) _ceilingDetector.Reset();
    }

    void LateUpdate() {
      //HandleRotation();
    }

    Vector3 CalculateMovementVelocity() => CalculateMovementDirection() * MovementSpeed;

    Vector3 CalculateMovementDirection() {
      Vector3 direction = _cameraTransform == null
          ? _tr.right * _input.Direction.x + _tr.forward * _input.Direction.y
          : Vector3.ProjectOnPlane(_cameraTransform.right, _tr.up).normalized * _input.Direction.x +
            Vector3.ProjectOnPlane(_cameraTransform.forward, _tr.up).normalized * _input.Direction.y;

      return direction.magnitude > 1f ? direction.normalized : direction;
    }

    void HandleRotation() {
      // Adjust the rotation to match the movement direction
      Vector3 velocity = Vector3.ProjectOnPlane(GetMovementVelocity(), _tr.up);
      if (velocity.magnitude < 0.001f) return;

      float angleDifference = VectorMath.GetAngle(_tr.forward, velocity.normalized, _tr.up);

      float step = Mathf.Sign(angleDifference) *
                   Mathf.InverseLerp(0f, _fallOffAngle, Mathf.Abs(angleDifference)) *
                   Time.deltaTime * TurnSpeed;

      CurrentYRotation += Mathf.Abs(step) > Mathf.Abs(angleDifference) ? angleDifference : step;

      _tr.localRotation = Quaternion.Euler(0f, CurrentYRotation, 0f);
    }

    void HandleMomentum() {
      if (UseLocalMomentum) _momentum = _tr.localToWorldMatrix * _momentum;

      Vector3 verticalMomentum = VectorMath.ExtractDotVector(_momentum, _tr.up);
      Vector3 horizontalMomentum = _momentum - verticalMomentum;

      verticalMomentum -= _tr.up * (Gravity * Time.deltaTime);
      if (_stateMachine.CurrentState is GroundedState && VectorMath.GetDotProduct(verticalMomentum, _tr.up) < 0f) {
        verticalMomentum = Vector3.zero;
      }

      if (!IsGrounded()) {
        AdjustHorizontalMomentum(ref horizontalMomentum, CalculateMovementVelocity());
      }

      if (_stateMachine.CurrentState is SlidingState) {
        HandleSliding(ref horizontalMomentum);
      }

      float friction = _stateMachine.CurrentState is GroundedState ? GroundFriction : AirFriction;
      horizontalMomentum = Vector3.MoveTowards(horizontalMomentum, Vector3.zero, friction * Time.deltaTime);

      _momentum = horizontalMomentum + verticalMomentum;

      if (_stateMachine.CurrentState is JumpingState) {
        HandleJumping();
      }

      if (_stateMachine.CurrentState is SlidingState) {
        _momentum = Vector3.ProjectOnPlane(_momentum, _mover.GetGroundNormal());
        if (VectorMath.GetDotProduct(_momentum, _tr.up) > 0f) {
          _momentum = VectorMath.RemoveDotVector(_momentum, _tr.up);
        }

        Vector3 slideDirection = Vector3.ProjectOnPlane(-_tr.up, _mover.GetGroundNormal()).normalized;
        _momentum += slideDirection * (SlideGravity * Time.deltaTime);
      }

      if (UseLocalMomentum) _momentum = _tr.worldToLocalMatrix * _momentum;
    }

    void HandleJumping() {
      _momentum = VectorMath.RemoveDotVector(_momentum, _tr.up);
      _momentum += _tr.up * JumpSpeed;
    }

    public void OnJumpStart() {
      if (UseLocalMomentum) _momentum = _tr.localToWorldMatrix * _momentum;

      _momentum += _tr.up * JumpSpeed;
      _jumpTimer.Start();
      _jumpInputIsLocked = true;
      OnJump.Invoke(_momentum);

      if (UseLocalMomentum) _momentum = _tr.worldToLocalMatrix * _momentum;
    }

    public void OnGroundContactLost() {
      if (UseLocalMomentum) _momentum = _tr.localToWorldMatrix * _momentum;

      Vector3 velocity = GetMovementVelocity();
      if (velocity.sqrMagnitude >= 0f && _momentum.sqrMagnitude > 0f) {
        Vector3 projectedMomentum = Vector3.Project(_momentum, velocity.normalized);
        float dot = VectorMath.GetDotProduct(projectedMomentum.normalized, velocity.normalized);

        if (projectedMomentum.sqrMagnitude >= velocity.sqrMagnitude && dot > 0f) velocity = Vector3.zero;
        else if (dot > 0f) velocity -= projectedMomentum;
      }
      _momentum += velocity;

      if (UseLocalMomentum) _momentum = _tr.worldToLocalMatrix * _momentum;
    }

    public void OnGroundContactRegained() {
      Vector3 collisionVelocity = UseLocalMomentum ? _tr.localToWorldMatrix * _momentum : _momentum;
      OnLand.Invoke(collisionVelocity);
    }

    public void OnFallStart() {
      var currentUpMomemtum = VectorMath.ExtractDotVector(_momentum, _tr.up);
      _momentum = VectorMath.RemoveDotVector(_momentum, _tr.up);
      _momentum -= _tr.up * currentUpMomemtum.magnitude;
    }

    void AdjustHorizontalMomentum(ref Vector3 horizontalMomentum, Vector3 movementVelocity) {
      if (horizontalMomentum.magnitude > MovementSpeed) {
        if (VectorMath.GetDotProduct(movementVelocity, horizontalMomentum.normalized) > 0f) {
          movementVelocity = VectorMath.RemoveDotVector(movementVelocity, horizontalMomentum.normalized);
        }
        horizontalMomentum += movementVelocity * (Time.deltaTime * AirControlRate * 0.25f);
      }
      else {
        horizontalMomentum += movementVelocity * (Time.deltaTime * AirControlRate);
        horizontalMomentum = Vector3.ClampMagnitude(horizontalMomentum, MovementSpeed);
      }
    }

    void HandleSliding(ref Vector3 horizontalMomentum) {
      Vector3 pointDownVector = Vector3.ProjectOnPlane(_mover.GetGroundNormal(), _tr.up).normalized;
      Vector3 movementVelocity = CalculateMovementVelocity();
      movementVelocity = VectorMath.RemoveDotVector(movementVelocity, pointDownVector);
      horizontalMomentum += movementVelocity * Time.fixedDeltaTime;
    }
  }
}