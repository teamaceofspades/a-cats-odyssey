using System;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
  // Movement
  [Header("Player")]
  [Tooltip("Move speed of the character in m/s")]
  public float MoveSpeed = 2.0f;
  [Tooltip("Sprint speed of the character in m/s")]
  public float SprintSpeed = 5.335f;
  [Tooltip("How fast the character turns to face movement direction")]
  [Range(0.0f, 0.3f)]
  public float RotationSmoothTime = 0.12f;
  [Tooltip("Acceleration and deceleration")]
  public float SpeedChangeRate = 10.0f;
  [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
  public float JumpTimeout = 0.0f;
  [Tooltip("The height the player can jump")]
  public float JumpHeight = 1.2f;
  public float SprintJumpHeight = 1.2f;
  [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
  public float Gravity = -9.81f;
  [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
  public float FallTimeout = 0.15f;
  [Header("Player Grounded")]
  [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
  public bool Grounded = true;
  [Tooltip("Useful for rough ground")]
  public float GroundedOffset = -0.14f;
  [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
  public float GroundedRadius = 0.28f;
  [Tooltip("What layers the character uses as ground")]
  public LayerMask GroundLayers;

  // player
  public GameObject Body;
  private float _speed;
  private float _animationBlend;
  private float _targetRotation = 0.0f;
  private float _rotationVelocity;
  private float _verticalVelocity;
  private readonly float _terminalVelocity = -53.0f;
  private bool _sprintWhileGrounded = false;
  private Vector2 _move;
  private Interactor _playerInteractor;
  private CharacterController _player;
  private PlayerInput playerInput;
  private InputAction _pi_sprint;
  private InputAction _pi_jump;

  // UI
  public TextMeshProUGUI InteractTextMeshPro;

  // Cameras
  public Camera MainCamera;
  public CinemachineCamera ThirdPersonCamera;
  public CinemachineCamera FirstPersonCamera;
  private bool _switchedCamera = false;
  private int _activeCameraPriorityModifier = 42069;

  // timeout deltatime
  private float _jumpTimeoutDelta;
  private float _fallTimeoutDelta;

  // animation
  private Animator _animator;
  private bool _hasAnimator;
  private int _animIDSpeed;
  private int _animIDGrounded;
  private int _animIDJump;
  private int _animIDFreeFall;
  private int _animIDMotionSpeed;

  void Start()
  {
    Cursor.SetCursor(PlayerSettings.defaultCursor, new Vector2(0, 0), CursorMode.ForceSoftware);
    Cursor.lockState = CursorLockMode.Locked;
    Body.SetActive(false);
    _player = GetComponent<CharacterController>();
    _hasAnimator = TryGetComponent(out _animator);
    _playerInteractor = GetComponent<Interactor>();
    GroundedRadius = _player.radius - 0.02f;
    // reset our timeouts on start
    _jumpTimeoutDelta = JumpTimeout;
    _fallTimeoutDelta = FallTimeout;

    // PlayerInputs to monitor
    playerInput = GetComponent<PlayerInput>();
    _pi_sprint = playerInput.actions["Sprint"];
    _pi_sprint.started += StartedSprint;
    _pi_sprint.canceled += CanceledSprint;
    _pi_jump = playerInput.actions["Jump"];
    FirstPersonCamera.Priority += _activeCameraPriorityModifier;
  }

  //private void AssignAnimationIDs()
  //{
  //  _animIDSpeed = Animator.StringToHash("Speed");
  //  _animIDGrounded = Animator.StringToHash("Grounded");
  //  _animIDJump = Animator.StringToHash("Jump");
  //  _animIDFreeFall = Animator.StringToHash("FreeFall");
  //  _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
  //}



  public void Update()
  {
    //_hasAnimator = TryGetComponent(out _animator);
    //if (_radius != 0f) { player.radius -= _radius; }
    JumpAndGravity();
    GroundedCheck();
    Move();
    _playerInteractor.UpdateInteractTextUI(InteractTextMeshPro);
  }

  /// <summary>
  /// Update Grounded, if player is/isn't grounded.
  /// Copied from the Unity Essentials Project.
  /// </summary>
  private void GroundedCheck()
  {
    // set sphere position, with offset
    Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y
      + GroundedOffset, transform.position.z);
    // True if on anything tagged GroundLayers
    Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
        QueryTriggerInteraction.Ignore);
    // update animator if using character
    if (_hasAnimator)
    {
      _animator.SetBool(_animIDGrounded, Grounded);
    }
  }

  /// <summary>
  /// Update jump/fall timers and apply gravity and possible jump.
  /// Copied from the Unity Essentials Project.
  /// </summary>
  private void JumpAndGravity()
  {
    if (Grounded)
    {
      // reset the fall timeout timer
      _fallTimeoutDelta = FallTimeout;

      // update animator if using character
      if (_hasAnimator)
      {
        _animator.SetBool(_animIDJump, false);
        _animator.SetBool(_animIDFreeFall, false);
      }

      // stop our velocity dropping infinitely when grounded
      if (_verticalVelocity < 0.0f)
      {
        _verticalVelocity = -2f;
      }

      // Jump
      if (_pi_jump.WasPressedThisFrame() && _jumpTimeoutDelta <= 0.0f)
      {
        // the square root of H * -2 * G = how much velocity needed to reach desired height
        _verticalVelocity = Mathf.Sqrt((_sprintWhileGrounded ? SprintJumpHeight : JumpHeight)
          * -2f * Gravity);

        // update animator if using character
        if (_hasAnimator)
        {
          _animator.SetBool(_animIDJump, true);
        }
      }

      // jump timeout
      if (_jumpTimeoutDelta >= 0.0f)
      {
        _jumpTimeoutDelta -= Time.deltaTime;
      }
    }
    else
    {
      if (_verticalVelocity > 0.0f)
      {
        if (_player.collisionFlags == CollisionFlags.Above)
        {
          _verticalVelocity = 0;
        }
        else if (_pi_jump.WasReleasedThisFrame())
        {
          // short jump
          _verticalVelocity *= .5f;
        }
      }

      // reset the jump timeout timer
      _jumpTimeoutDelta = JumpTimeout;

      // fall timeout
      if (_fallTimeoutDelta >= 0.0f)
      {
        _fallTimeoutDelta -= Time.deltaTime;
      }
      else
      {
        // update animator if using character
        if (_hasAnimator)
        {
          _animator.SetBool(_animIDFreeFall, true);
        }
      }
    }

    // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
    if (_verticalVelocity >= _terminalVelocity)
    {
      _verticalVelocity += Gravity * Time.deltaTime;
    }
  }

  /// <summary>
  /// Update movement of player.
  /// Copied from the Unity Essentials Project.
  /// </summary>
  private void Move()
  {
    // set target speed based on move speed, sprint speed and if sprint is pressed
    float targetSpeed;
    targetSpeed = _pi_sprint.IsPressed() && _sprintWhileGrounded ? SprintSpeed : MoveSpeed;

    // a simplistic acceleration and deceleration designed to be easy to remove, replace, or
    // iterate upon

    // note: Vector2's == operator uses approximation so is not floating point error prone,
    // and is cheaper than magnitude
    // if there is no input, set the target speed to 0
    if (_move == Vector2.zero) targetSpeed = 0.0f;

    // a reference to the players current horizontal velocity
    float currentHorizontalSpeed = new Vector3(_player.velocity.x, 0.0f,
      _player.velocity.z).magnitude;

    //float speedOffset = 0.1f;

    // accelerate or decelerate to target speed
    //if (currentHorizontalSpeed < targetSpeed - speedOffset ||
    //    currentHorizontalSpeed > targetSpeed + speedOffset)
    //{
    //  // creates curved result rather than a linear one giving a more organic speed change
    //  // note T in Lerp is clamped, so we don't need to clamp our speed
    //  _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * SpeedChangeRate);

    //  // round speed to 3 decimal places
    //  _speed = Mathf.Round(_speed * 1000f) / 1000f;
    //}
    //else
    //{
    //  _speed = targetSpeed;
    //}
    _speed = targetSpeed;
    _animationBlend = targetSpeed;
    //_animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
    //if (_animationBlend < 0.01f) _animationBlend = 0f;

    // normalise input direction
    Vector3 inputDirection = new Vector3(_move.x, 0.0f, _move.y).normalized;

    // note: Vector2's != operator uses approximation so is not floating point error prone,
    // and is cheaper than magnitude if there is a move input rotate player when the player
    // is moving
    if (_move != Vector2.zero)
    {
      _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                        MainCamera.transform.eulerAngles.y;
      float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation,
        ref _rotationVelocity, RotationSmoothTime);

      // rotate to face input direction relative to camera position
      transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
    }


    Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

    // move the player
    _player.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                     new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

    // update animator if using character
    if (_hasAnimator)
    {
      _animator.SetFloat(_animIDSpeed, _animationBlend);
      _animator.SetFloat(_animIDMotionSpeed, 1f);
    }
  }

  public void OnMove(InputValue value)
  {
    _move = value.Get<Vector2>();
  }

  private void StartedSprint(InputAction.CallbackContext context)
  {
    _sprintWhileGrounded = Grounded;
  }

  private void CanceledSprint(InputAction.CallbackContext context)
  {
    _sprintWhileGrounded = false;
  }

  public void OnChangeCamera()
  {
    if (_switchedCamera)
    {
      //SetCameraPriority(FirstPersonCamera, ThirdPersonCamera);
      SetCameraPriority(ThirdPersonCamera, FirstPersonCamera);
      Body.gameObject.SetActive(false);
      _switchedCamera = false;
    }
    else
    {
      SetCameraPriority(FirstPersonCamera, ThirdPersonCamera);
      //SetCameraPriority(ThirdPersonCamera, FirstPersonCamera);
      _switchedCamera = true;
      Body.gameObject.SetActive(true);
    }
  }

  private void SetCameraPriority(CinemachineCamera oldCamera, CinemachineCamera newCamera)
  {
    oldCamera.Priority -= _activeCameraPriorityModifier;
    newCamera.Priority += _activeCameraPriorityModifier;
  }

  public void OnPauseMenu()
  {
    // Pause World

    // Enable Menu GUI

    // Unlock Cursor
    Cursor.lockState = CursorLockMode.None;
  }
}
