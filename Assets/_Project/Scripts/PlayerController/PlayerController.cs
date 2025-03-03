using RealityWard.EventSystem;
using RealityWard.StateMachineSystem;
using RealityWard.Utilities.Helpers;
using System;
using UnityEngine;

namespace PlayerController {
  [RequireComponent(typeof(PlayerMover))]
  public class PlayerController : MonoBehaviour {
    #region Fields
    [SerializeField] private InputReader _input;
    [SerializeField] private float _movementSpeed = 7f;
    [SerializeField] private float _turnSpeed = 50f;
    [SerializeField] private float _airControlRate = 2f;
    [SerializeField] private float _jumpSpeed = 10f;
    [SerializeField] private float _jumpDuration = 0.2f;
    [SerializeField] private float _airFriction = 0.5f;
    [SerializeField] private float _groundFriction = 100f;
    [SerializeField] private float _gravity = -30;
    [SerializeField] private float _slideGravity = -5f;
    [SerializeField] private float _slopeLimit = 30f;
    [SerializeField] private bool _useLocalMomentum = true;

    private Transform _tr;
    private Animator _animator;
    private PlayerMover _mover;
    private CeilingDetector _ceilingDetector;

    private bool JumpKeyIsPressed() => _input.InputActions.Player.Jump.IsPressed();
    private bool JumpKeyWasPressed() => _input.InputActions.Player.Jump.WasPressedThisFrame();
    private bool JumpKeyWasLetGo() => _input.InputActions.Player.Jump.WasReleasedThisFrame();
    private bool _jumpInputIsLocked;

    public float MovementSpeed {
      get { return _movementSpeed; }
      set {
        if (value >= 0) _movementSpeed = value;
        else _movementSpeed = 0;
      }
    }
    public float TurnSpeed {
      get { return _turnSpeed; }
      set {
        if (value >= 0) _turnSpeed = value;
        else _turnSpeed = 0;
      }
    }
    public float AirControlRate {
      get { return _airControlRate; }
      set {
        if (value >= 0) _airControlRate = value;
        else _airControlRate = 0;
      }
    }
    public float JumpSpeed {
      get { return _jumpSpeed; }
      set {
        if (value >= 0) _jumpSpeed = value;
        else _jumpSpeed = 0;
      }
    }
    public float JumpDuration {
      get { return _jumpDuration; }
      set {
        if (value >= 0) _jumpDuration = value;
        else _jumpDuration = 0;
      }
    }
    public float AirFriction {
      get { return _airFriction; }
      set {
        if (value >= 0) _airFriction = value;
        else _airFriction = 0;
      }
    }
    public float GroundFriction {
      get { return _groundFriction; }
      set {
        if (value >= 0) _groundFriction = value;
        else _groundFriction = 0;
      }
    }
    public float Gravity {
      get { return _gravity; }
      set {
        if (value <= 0) _gravity = value;
        else _gravity = 0;
      }
    }
    public float SlideGravity {
      get { return _slideGravity; }
      set {
        if (value <= 0) _slideGravity = value;
        else _slideGravity = 0;
      }
    }
    public float SlopeLimit {
      get { return _slopeLimit; }
      set {
        if (value >= 0 && value < 90f) _slopeLimit = value;
        else _slopeLimit = 0;
      }
    }
    public bool UseLocalMomentum {
      get { return _useLocalMomentum; }
      private set { _useLocalMomentum = value; }
    }
    public float CurrentYRotation { get; private set; }
    public const float FallOffAngle = 90f;

    private StateMachine _stateMachine;
    private CountdownTimer _jumpTimer;

    [SerializeField] private Transform _cameraTransform;

    private Vector3 _momentum, _savedVelocity, _savedMovementVelocity;

    public event Action<Vector3> OnJump = delegate { };
    public event Action<Vector3> OnLand = delegate { };
    #endregion

    private bool IsGrounded() => _stateMachine.CurrentState is GroundedState or SlidingState;
    public Vector3 GetVelocity() => _savedVelocity;
    public Vector3 GetMomentum() => UseLocalMomentum ? _tr.localToWorldMatrix * _momentum : _momentum;
    public Vector3 GetMovementVelocity() => _savedMovementVelocity;

    private void OnValidate() {
      MovementSpeed = MovementSpeed;
      TurnSpeed = TurnSpeed;
      AirControlRate = AirControlRate;
      JumpSpeed = JumpSpeed;
      JumpDuration = JumpDuration;
      AirFriction = AirFriction;
      GroundFriction = GroundFriction;
      Gravity = Gravity;
      SlideGravity = SlideGravity;
      SlopeLimit = SlopeLimit;
    }

    private void Awake() {
      _tr = transform;
      _mover = GetComponent<PlayerMover>();
      _animator = GetComponent<Animator>();
      _ceilingDetector = GetComponent<CeilingDetector>();
      CurrentYRotation = _tr.localEulerAngles.y;

      _jumpTimer = new CountdownTimer(JumpDuration);
      SetupStateMachine();
    }

    private void Start() {
      _input.EnablePlayerActions();
      _input.Jump += HandleJumpKeyInput;
    }

    private void HandleJumpKeyInput(bool isButtonPressed) {
      if (JumpKeyIsPressed() && !isButtonPressed) {
        _jumpInputIsLocked = false;
      }
    }

    private void SetupStateMachine() {
      _stateMachine = new StateMachine();

      var grounded = new GroundedState(this, _animator);
      var falling = new FallingState(this, _animator);
      var sliding = new SlidingState(this, _animator);
      var rising = new RisingState(this, _animator);
      var jumping = new JumpingState(this, _animator);

      At(grounded, rising, () => IsRising());
      At(grounded, sliding, () => _mover.IsGrounded() && IsGroundTooSteep());
      At(grounded, falling, () => !_mover.IsGrounded());
      At(grounded, jumping, () => (JumpKeyIsPressed() || JumpKeyWasPressed()) && !_jumpInputIsLocked);

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

      At(jumping, rising, () => _jumpTimer.IsFinished || JumpKeyWasLetGo());
      At(jumping, falling, () => _ceilingDetector != null && _ceilingDetector.HitCeiling());

      _stateMachine.SetState(falling);
    }

    private void At(IState from, IState to, Func<bool> condition) => _stateMachine.AddTransition(from, to, condition);
    private void Any<T>(IState to, Func<bool> condition) => _stateMachine.AddAnyTransition(to, condition);

    private bool IsRising() => VectorMath.GetDotProduct(GetMomentum(), Vector3.up) > 0f;
    private bool IsFalling() => VectorMath.GetDotProduct(GetMomentum(), Vector3.up) < 0f;
    private bool IsGroundTooSteep() => !_mover.IsGrounded() || Vector3.Angle(_mover.GetAverageGroundNormal(), Vector3.up) > SlopeLimit;

    private void Update() => _stateMachine.Update();

    private void FixedUpdate() {
      _stateMachine.FixedUpdate();
      _mover.CheckGroundAdjustment();
      HandleMomentum();
      Vector3 velocity = _stateMachine.CurrentState is GroundedState ? CalculateMovementVelocity() : Vector3.zero;
      velocity += UseLocalMomentum ? _tr.localToWorldMatrix * _momentum : _momentum;

      _mover.SetVelocity(velocity);
      //_mover.SetVelocity(Vector3.zero);

      _savedVelocity = velocity;
      _savedMovementVelocity = CalculateMovementVelocity();

      if (_ceilingDetector != null) _ceilingDetector.Reset();
    }

    private void LateUpdate() {
      HandleRotation();
    }

    private Vector3 CalculateMovementVelocity() => CalculateMovementDirection() * MovementSpeed;

    private Vector3 CalculateMovementDirection() {
      Vector3 direction = _cameraTransform == null
          ? Vector3.ProjectOnPlane(_tr.right, Vector3.up).normalized * _input.Direction.x + Vector3.ProjectOnPlane(_tr.forward, Vector3.up).normalized * _input.Direction.y
          : Vector3.ProjectOnPlane(_cameraTransform.right, Vector3.up).normalized * _input.Direction.x +
            Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized * _input.Direction.y;

      return direction.normalized;
    }

    private void HandleRotation() {
      // Adjust the rotation to match the movement direction
      Vector3 velocity = Vector3.ProjectOnPlane(GetMovementVelocity(), Vector3.up);
      if (velocity.magnitude < 0.001f) return;

      float angleDifference = VectorMath.GetAngle(Vector3.ProjectOnPlane(_tr.forward, Vector3.up).normalized, velocity.normalized, Vector3.up);

      float step = Mathf.Sign(angleDifference) *
                   Mathf.InverseLerp(0f, FallOffAngle, Mathf.Abs(angleDifference)) *
                   Time.deltaTime * TurnSpeed;

      CurrentYRotation += Mathf.Abs(step) > Mathf.Abs(angleDifference) ? angleDifference : step;

      _tr.localRotation = Quaternion.Euler(0f, CurrentYRotation, 0f);
    }

    private void HandleMomentum() {
      if (UseLocalMomentum) _momentum = _tr.localToWorldMatrix * _momentum;

      Vector3 verticalMomentum = VectorMath.ExtractDotVector(_momentum, Vector3.up);
      Vector3 horizontalMomentum = _momentum - verticalMomentum;

      verticalMomentum += Vector3.up * (Gravity * Time.deltaTime);
      if (_stateMachine.CurrentState is GroundedState && VectorMath.GetDotProduct(verticalMomentum, Vector3.up) < 0f) {
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
        _momentum = Vector3.ProjectOnPlane(_momentum, _mover.GetAverageGroundNormal());
        if (VectorMath.GetDotProduct(_momentum, Vector3.up) > 0f) {
          _momentum = VectorMath.RemoveDotVector(_momentum, Vector3.up);
        }

        Vector3 slideDirection = Vector3.ProjectOnPlane(-Vector3.up, _mover.GetAverageGroundNormal()).normalized;
        _momentum += slideDirection * (SlideGravity * Time.deltaTime);
      }

      if (UseLocalMomentum) _momentum = _tr.worldToLocalMatrix * _momentum;
    }

    private void HandleJumping() {
      _momentum = VectorMath.RemoveDotVector(_momentum, Vector3.up);
      _momentum += Vector3.up * JumpSpeed;
    }

    public void OnJumpStart() {
      if (UseLocalMomentum) _momentum = _tr.localToWorldMatrix * _momentum;

      _momentum += Vector3.up * JumpSpeed;
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
      var currentUpMomemtum = VectorMath.ExtractDotVector(_momentum, Vector3.up);
      _momentum = VectorMath.RemoveDotVector(_momentum, Vector3.up);
      _momentum -= Vector3.up * currentUpMomemtum.magnitude;
    }

    private void AdjustHorizontalMomentum(ref Vector3 horizontalMomentum, Vector3 movementVelocity) {
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

    private void HandleSliding(ref Vector3 horizontalMomentum) {
      Vector3 pointDownVector = Vector3.ProjectOnPlane(_mover.GetAverageGroundNormal(), Vector3.up).normalized;
      Vector3 movementVelocity = CalculateMovementVelocity();
      movementVelocity = VectorMath.RemoveDotVector(movementVelocity, pointDownVector);
      horizontalMomentum += movementVelocity * Time.fixedDeltaTime;
    }
  }
}