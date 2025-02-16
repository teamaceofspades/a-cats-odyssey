using System;
using TMPro;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using static UnityEditor.PlayerSettings;

public class PlayerController : MonoBehaviour
{
  #region movement
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
  [Tooltip("If the character is grounded or not. Not part of the AnimalController built in grounded check")]
  public bool Grounded = true;
  [Tooltip("Useful for rough ground")]
  public float GroundedOffset = -0.14f;
  [Tooltip("The radius of the grounded check. Should match the radius of the AnimalController")]
  public float GroundedRadius = 0.28f;
  [Tooltip("What layers the character uses as ground")]
  public LayerMask GroundLayers;

  [SerializeField]
  private CapsuleCollider _capsule;
  public CapsuleCollider Capsule { get { return _capsule; } }

  private Vector3 _velocity;
  public Vector3 Velocity { get { return _velocity; } }

  private CollisionFlags _collisionFlags;
  public CollisionFlags collisionFlags { get { return _collisionFlags; } }

  public bool DetectCollisions = true;

  public bool EnableOverlapRecovery = true;

  [SerializeField]
  private bool _isGrounded = false;
  public bool IsGrounded { get { return _isGrounded; } }

  [SerializeField]
  private float _skinWidth = 0.001f;
  public float SkinWidth { get { return _skinWidth; } }
  public void SetSkinWidth(float value)
  {
    if (value < _capsule.radius && value >= 0.001f)
    {
      _skinWidth = value;
    }
  }

  [SerializeField]
  private float _stepOffset = 0.1f;
  public float StepOffset { get { return _stepOffset; } }
  public void SetStepOffset(float value)
  {
    if (value < _capsule.height && value >= 0.1f)
    {
      _stepOffset = value;
    }
  }

  [SerializeField]
  private float _slopeLimit = 55f;
  public float SlopeLimit { get { return _slopeLimit; } }
  public void SetSlopeLimit(float value)
  {
    if (value < 90f && value >= 1f)
    {
      _slopeLimit = value;
    }
  }

  private Bounds _bounds;
  private float _speed;
  private float _animationBlend;
  private float _targetRotation = 0.0f;
  private float _targetSpeed;
  private float _rotationVelocity;
  private float _verticalVelocity;
  private readonly float _terminalVelocity = -53.0f;
  private bool _sprintWhileGrounded = false;
  private readonly int _maxCollideAndSlides = 5;
  private Vector2 _move;
  #endregion

  // player
  public GameObject Body;
  private Interactor _playerInteractor;
  private PlayerInput playerInput;
  private InputAction _pi_sprint;
  private InputAction _pi_jump;

  // UI
  [Header("UI")]
  public TextMeshProUGUI InteractTextMeshPro;
  public GameObject PausePanel;
  public Texture2D MouseTexture;

  // Cameras
  [Header("Cameras")]
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
    VerifyProperties();
    _bounds = _capsule.bounds;
    _bounds.Expand(-2 * _skinWidth);
    Cursor.SetCursor(MouseTexture, new Vector2(0, 0), CursorMode.ForceSoftware);
    Cursor.visible = false;
    Cursor.lockState = CursorLockMode.Locked;
    _hasAnimator = TryGetComponent(out _animator);
    _playerInteractor = GetComponent<Interactor>();
    // reset our timeouts on start
    _jumpTimeoutDelta = JumpTimeout;
    _fallTimeoutDelta = FallTimeout;

    // PlayerInputs to monitor
    playerInput = GetComponent<PlayerInput>();
    _pi_sprint = playerInput.actions["Sprint"];
    _pi_sprint.started += StartedSprint;
    _pi_sprint.canceled += CanceledSprint;
    _pi_jump = playerInput.actions["Jump"];
  }

  /// <summary>
  /// Verifies properties that can be set out of bounds in editor and defaults them if they are.
  /// </summary>
  private void VerifyProperties()
  {
    if (!(_slopeLimit < 90f && _slopeLimit >= 1f))
    {
      _slopeLimit = 45f;
      Debug.Log("Slope Limit out of bounds (_slopeLimit < 90f && _slopeLimit >= 1f). Set to 45f.");
    }
    if (!(_stepOffset <= _capsule.height && _stepOffset >= 0.1f))
    {
      _stepOffset = 0.1f;
      Debug.Log($"Step Offset out of bounds (_stepOffset <= {_capsule.height} && _stepOffset >= 0.1f). Set to 0.1f.");
    }
    if (!(_skinWidth < _capsule.radius && _skinWidth >= 0.001f))
    {
      _skinWidth = 0.001f;
      Debug.Log($"Skin Width out of bounds (_skinWidth < {_capsule.radius} && _skinWidth >= 0.001f). Set to 0.001f.");
    }
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
    //JumpAndGravity();
    //GroundedCheck();
    Move();
    //_playerInteractor.UpdateInteractTextUI(InteractTextMeshPro);
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
    /*
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
          if (_verticalvelocity < 0.0f)
          {
            _verticalvelocity = -2f;
          }

          // Jump
          if (_pi_jump.WasPressedThisFrame() && _jumpTimeoutDelta <= 0.0f)
          {
            // the square root of H * -2 * G = how much velocity needed to reach desired height
            _verticalvelocity = Mathf.Sqrt((_sprintWhileGrounded ? SprintJumpHeight : JumpHeight)
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
          if (_verticalvelocity > 0.0f)
          {
            if (_player.collisionFlags == CollisionFlags.Above)
            {
              _verticalvelocity = 0;
            }
            else if (_pi_jump.WasReleasedThisFrame())
            {
              // short jump
              _verticalvelocity *= .5f;
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
        if (_verticalvelocity >= _terminalvelocity)
        {
          _verticalvelocity += Gravity * Time.deltaTime;
        }
    */
  }

  /// <summary>
  /// Update movement of player.<br/>
  /// Modified from the Unity Essentials Project 2025.
  /// </summary>
  private void Move()
  {
    // if there is no input, set the target speed to 0
    if (_move == Vector2.zero) { _targetSpeed = 0.0f; }
    else
    {
      // set target speed based on move speed, sprint speed and if sprint is pressed
      _targetSpeed = _pi_sprint.IsPressed() && _sprintWhileGrounded ? SprintSpeed : MoveSpeed;
      Vector3 inputDirection = new Vector3(_move.x, 0.0f, _move.y).normalized;
      _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                        MainCamera.transform.eulerAngles.y;
      float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation,
        ref _rotationVelocity, RotationSmoothTime);

      // rotate to face input direction relative to camera position
      transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
    }
    _speed = _targetSpeed;
    Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
    targetDirection = targetDirection.normalized * (_speed * Time.deltaTime) +
      new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;
    transform.position += CollideAndSlide(targetDirection, transform.position, 0, false, targetDirection);
  }

  /// <summary>
  /// Character Collision movement.<br/>
  /// Credit: youtube video "Collide And Slide - *Actually
  /// Decent* Character Collision From Scratch" by Poke Dev<br/>
  /// Link: https://www.youtube.com/watch?v=YR6Q7dUz2uk
  /// <br/> with slight modifications.
  /// </summary>
  /// <param name="vel">Velocity/Move Amount</param>
  /// <param name="pos">Current Position</param>
  /// <param name="depth">Recurrsion Depth</param>
  /// <param name="gravityPass">Is this a gravity velocity?</param>
  /// <param name="velInit">Velocity/Move Amount</param>
  /// <returns>Transformed Velocity/Move Amount</returns>
  private Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit)
  {
    if (depth >= _maxCollideAndSlides)
    {
      // Break recursion loop
      return Vector3.zero;
    }

    float dist = vel.magnitude + _skinWidth;
    // Get the axis the capsule is aligned to
    Vector3 front = new(0, 0, 0);
    switch (_capsule.direction)
    {
      case 0:
        front.x = 1;
        break;
      case 1:
        front.y = 1;
        break;
      case 2:
        front.z = 1;
        break;
    }
    // Top Center
    Vector3 pos1 = transform.position + _capsule.center + (front * (-_capsule.height + _capsule.radius * 2) * 0.5f);
    // Bottom Center
    Vector3 pos2 = pos1 + (front * (_capsule.height - _capsule.radius * 2));

    if (Physics.CapsuleCast(pos1, pos2, _bounds.extents.x, vel.normalized, out RaycastHit hit, dist, LayerMask.GetMask("Ground", "Default")))
    {
      // Distance to the collided surface
      Vector3 snapToSurface = vel.normalized * (hit.distance - _skinWidth);
      // Distance past the collided surface
      Vector3 leftover = vel - snapToSurface;
      float angle = Vector3.Angle(Vector3.up, hit.normal);

      // Check if distance is too short
      if (snapToSurface.magnitude <= _skinWidth)
      {
        snapToSurface = Vector3.zero;
      }

      // normal ground or traversable slope
      if (angle <= _slopeLimit)
      {
        if (gravityPass)
        {
          return snapToSurface;
        }
        leftover = ProjectAndScale(leftover, hit.normal);
      }
      // wall or steep slope
      else
      {
        // Reduce effectiveness of movement directly into surfaces
        float scale = 1 - Vector3.Dot(
          new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
          -new Vector3(velInit.x, 0, velInit.z).normalized
          );
        if (_isGrounded && !gravityPass)
        {
          leftover = ProjectAndScale(
            new Vector3(leftover.x, 0, leftover.z).normalized,
            -new Vector3(hit.normal.x, 0, hit.normal.z).normalized
            ) * scale;
        }
        else
        {
          leftover = ProjectAndScale(leftover, hit.normal) * scale;
        }
      }
      // Return reduced velocity + the next level of recursion
      return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass, velInit);
    }
    // No collision found
    return vel;
  }

  private void OnDrawGizmos()
  {
    // Get the axis the capsule is aligned to
    Vector3 front = new(0, 0, 0);
    switch (_capsule.direction)
    {
      case 0:
        front.x = 1;
        break;
      case 1:
        front.y = 1;
        break;
      case 2:
        front.z = 1;
        break;
    }
    // Top Center
    Vector3 pos1 = transform.position + _capsule.center + (front * (-_capsule.height + _capsule.radius * 2) * 0.5f);
    // Bottom Center
    Vector3 pos2 = pos1 + (front * (_capsule.height - _capsule.radius * 2));
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(pos1, _capsule.radius);
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(pos2, _capsule.radius);
  }
  /// <summary>
  /// Transforms the vector perpendicular to the normal.
  /// </summary>
  /// <param name="vec">Vector to transform</param>
  /// <param name="normal">The normal of the surface to deflect on</param>
  /// <returns>vector perpendicular to the normal</returns>
  private Vector3 ProjectAndScale(Vector3 vec, Vector3 normal)
  {
    float mag = vec.magnitude;
    vec = Vector3.ProjectOnPlane(vec, normal).normalized;
    vec *= mag;
    return vec;
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
    if (PausePanel.activeSelf)
    {
      PausePanel.GetComponent<PauseMenu>().OnUnpause();
    }
    else
    {
      PausePanel.GetComponent<PauseMenu>().Pause();
    }
  }

  public void OnInteract()
  {
    if (!PausePanel.activeSelf)
    {
      GetComponent<Interactor>().Interact();
    }
  }
}
