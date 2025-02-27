using RealityWard.EventSystem;
using KBCore.Refs;
using NUnit.Framework;
using RealityWard.StateMachineSystem;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using RealityWard.PlayerController;
using UnityEngine.EventSystems;

namespace RealityWard.PlayerController {
  public class PlayerController : ValidatedMonoBehaviour {
    #region Fields
    [Header("References")]
    [SerializeField, Self] Rigidbody _rb;
    [SerializeField, Self] GroundChecker _groundChecker;
    [SerializeField, Self] Animator _animator;
    [SerializeField, Self] HealthEventModule _health;
    [SerializeField, Anywhere] InputReader _input;
    [SerializeField, Anywhere] CinemachineCamera _freeLookVCam;
    [SerializeField, Anywhere] Transform _CameraTarget;
    Transform _mainCam;

    [Header("Movement Settings")]
    [SerializeField] float _moveSpeed = 300f;
    public float MoveSpeed {
      get { return _moveSpeed; }
      set { if (value >= 0) _moveSpeed = value; }
    }
    float _moveSpeedMult = 1f;
    public float MoveSpeedMult {
      get { return _moveSpeedMult; }
      set { if (value >= 0) _moveSpeedMult = value; }
    }
    [SerializeField] float _sprintSpeedMult = 1.5f;
    public float SprintSpeedMult {
      get { return _sprintSpeedMult; }
      private set {
        if (value > 0) _sprintSpeedMult = value;
      }
    }
    [SerializeField] float _rotationSpeed = 400f;
    [SerializeField] float _smoothTime = 0.2f;
    Vector3 _movement;
    float _currentSpeed;
    float _velocity;

    [Header("Jump Settings")]
    [SerializeField] float _jumpForce = 10f;
    [SerializeField] float _jumpDuration = 0.5f;
    [SerializeField] float _jumpCooldown = 0f;
    [SerializeField] float _jumpGravityMultiplier = 3f;
    [SerializeField] float _terminalVelocity = 50f;
    float _jumpVelocity;

    [Header("Attack Settings")]
    [SerializeField] float _attackCooldown = 0.5f;
    [SerializeField] int _damageAmount = 10;
    [SerializeField] float _attackDistance = 1f;

    // Animator parameters
    static readonly int _a_Speed = Animator.StringToHash("Speed");

    List<Timer> _timers;
    CountdownTimer _jumpTimer;
    CountdownTimer _jumpCooldownTimer;
    CountdownTimer _attackTimer;

    StateMachine _stateMachine;
    const float ZeroF = 0f;
    #endregion

    #region Object Initialization
    private void Awake() {
      _mainCam = Camera.main.transform;
      _freeLookVCam.Follow = _CameraTarget;
      _freeLookVCam.LookAt = _CameraTarget;
      _freeLookVCam.OnTargetObjectWarped(_CameraTarget, _CameraTarget.position -
        _freeLookVCam.transform.position - Vector3.forward);

      _rb.freezeRotation = true;

      SetupTimers();
      SetupStateMachine();
    }

    private void Start() {
      _input.EnablePlayerActions();
    }

    private void OnEnable() {
      _input.Jump += OnJump;
      _input.Attack += OnAttack;
    }

    private void OnDisable() {
      _input.Jump -= OnJump;
      _input.Attack -= OnAttack;
    }

    private void SetupTimers() {
      // Jump Timers
      _jumpTimer = new(_jumpDuration);
      _jumpCooldownTimer = new(_jumpCooldown);

      _jumpTimer.OnTimerStart += () => _jumpVelocity = _jumpForce;
      _jumpTimer.OnTimerStop += () => _jumpCooldownTimer.Start();
      //Attack Timers
      _attackTimer = new(_attackCooldown);

      _timers = new List<Timer>(3) { _jumpTimer, _jumpCooldownTimer, _attackTimer };
    }

    private void SetupStateMachine() {
      _stateMachine = new StateMachine();

      // Declare States
      var locomotionState = new LocomotionState(this, _animator);
      var jumpState = new JumpState(this, _animator);
      var sprintState = new SprintState(this, _animator);
      var attackState = new AttackState(this, _animator);

      // Define Transitions
      At(locomotionState, jumpState, new FuncPredicate(() => _jumpTimer.IsRunning));
      At(locomotionState, sprintState, new FuncPredicate(() => _input.IsSprintKeyPressed()));
      At(sprintState, jumpState, new FuncPredicate(() => _jumpTimer.IsRunning));
      At(locomotionState, attackState, new FuncPredicate(() => _attackTimer.IsRunning));
      Any(locomotionState,
        new FuncPredicate(ReturnToLocomotionState));


      // Set initial state
      _stateMachine.SetState(locomotionState);
    }

    private bool ReturnToLocomotionState() {
      return _groundChecker.IsGrounded
        && !_attackTimer.IsRunning
        && !_input.IsSprintKeyPressed()
        && !_jumpTimer.IsRunning;
    }

    void At(IState from, IState to, IPredicate condition) => _stateMachine.AddTransition(from, to, condition);
    void Any(IState to, IPredicate condition) => _stateMachine.AddAnyTransition(to, condition);
    #endregion

    #region Unity
    private void Update() {
      _movement = new Vector3(_input.Direction.x, 0f, _input.Direction.y);
      _stateMachine.Update();
      Timer.HandleTimers(_timers);
      UpdateAnimator();
    }

    private void UpdateAnimator() {
      //_animator.SetFloat(_a_Speed, _currentSpeed);
    }

    private void FixedUpdate() {
      _stateMachine.FixedUpdate();
    }
    #endregion

    #region Movement
    public void HandleJump() {
      // if not jumping and grounded, keep jump velocity at 0f
      if (!_jumpTimer.IsRunning && _groundChecker.IsGrounded) {
        _jumpVelocity = ZeroF;
        return;
      }
      // falling calc velocity
      if (!_jumpTimer.IsRunning && _jumpVelocity < _terminalVelocity) {
        // Gravity takes over
        _jumpVelocity += Physics.gravity.y * _jumpGravityMultiplier * Time.fixedDeltaTime;
        if (_jumpVelocity > _terminalVelocity) { _jumpVelocity = _terminalVelocity; }
      }

      // Apply velocity
      _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _jumpVelocity, _rb.linearVelocity.z);
    }

    public void HandleMovement() {
      // rotate to match camera direction
      Vector3 adjustedDirection = Quaternion.AngleAxis(_mainCam.eulerAngles.y,
        Vector3.up) * _movement.normalized;
      if (adjustedDirection.magnitude > ZeroF) {
        HandleRotation(adjustedDirection);
        HandleHorizontalMovement(adjustedDirection);
        SmootSpeed(adjustedDirection.magnitude);
      }
      else {
        SmootSpeed(ZeroF);

        // Snappy Stopping power
        _rb.linearVelocity = new Vector3(ZeroF, _rb.linearVelocity.y, ZeroF);
      }
    }

    private void HandleHorizontalMovement(Vector3 adjustedDirection) {
      // Move the player
      Vector3 velocity = adjustedDirection * (_moveSpeed * _moveSpeedMult * Time.fixedDeltaTime);
      _rb.linearVelocity = new Vector3(velocity.x, _rb.linearVelocity.y, velocity.z);
    }

    private void HandleRotation(Vector3 adjustedDirection) {
      // Adjust the rotation to match the movement direction
      Quaternion targetRotation = Quaternion.LookRotation(adjustedDirection);
      transform.rotation = Quaternion.RotateTowards(transform.rotation,
        targetRotation, _rotationSpeed * Time.deltaTime);
    }

    void SmootSpeed(float value) {
      _currentSpeed = Mathf.SmoothDamp(_currentSpeed, value,
          ref _velocity, _smoothTime);
    }
    #endregion

    #region Events
    public void OnJump(bool performed) {
      if (performed && !_jumpTimer.IsRunning && !_jumpCooldownTimer.IsRunning &&
        _groundChecker.IsGrounded) {
        _jumpTimer.Start();
      }
      else if (!performed && _jumpTimer.IsRunning) {
        _jumpTimer.Stop();
      }
    }

    private void OnAttack() {
      if (!_attackTimer.IsRunning) {
        _attackTimer.Start();
      }
    }

    public void Attack() {
      Vector3 attackPos = transform.position + transform.forward;
      Collider[] hitEnemies = Physics.OverlapSphere(attackPos, _attackDistance);

      foreach (var enemy in hitEnemies) {
        Debug.Log(enemy.name);
        if (enemy.CompareTag("Enemy")) {
          enemy.gameObject.GetComponent<HealthEventModule>().TakeDamage(_damageAmount);
        }
      }
    }
    #endregion
  }
}