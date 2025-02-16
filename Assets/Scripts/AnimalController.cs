using UnityEngine;
using UnityEngine.LowLevelPhysics;
using static UnityEngine.Rendering.DebugUI;

public class AnimalController : MonoBehaviour
{
  #region PubVars
  [SerializeField]
  private CapsuleCollider _capsule;
  public CapsuleCollider Capsule { get { return _capsule; } }

  private Vector3 _velocity;
  public Vector3 velocity { get { return _velocity; } }

  private CollisionFlags _collisionFlags;
  public CollisionFlags collisionFlags { get { return _collisionFlags; } }

  public bool DetectCollisions = true;

  public bool EnableOverlapRecovery = true;

  [SerializeField]
  private bool _isGrounded = false;
  public bool IsGrounded { get { return _isGrounded; } }

  [SerializeField]
  private float _minMoveDistance = 0.001f;
  public float MinMoveDistance { get { return _minMoveDistance; } }
  public void SetMinMoveDistance(float value)
  {
    if (value >= 0.001f)
    {
      _minMoveDistance = value;
    }
  }

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
  private float _slopeLimit = 45f;
  public float SlopeLimit { get { return _slopeLimit; } }
  public void SetSlopeLimit(float value)
  {
    if (value < 90f && value >= 1f)
    {
      _slopeLimit = value;
    }
  }
  #endregion

  private Vector3[] _posHistory = new Vector3[4];
  private int _posHistoryIndex = 0;

  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Start()
  {
    VerifyProperties();
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

  // Update is called once per frame
  void Update()
  {
  }

  private void UpdatePositionHistory()
  {
    _posHistoryIndex++;
    if (_posHistoryIndex >= _posHistory.Length)
    {
      _posHistoryIndex = 0;
    }
    _posHistory[_posHistoryIndex] = transform.position;
  }

  private Vector3 PreviousPosition()
  {
    return PreviousPosition(_posHistoryIndex);
  }

  private Vector3 PreviousPosition(int curIndex)
  {
    if (curIndex - 1 < 0)
    {
      curIndex = _posHistory.Length - 1;
    }
    return _posHistory[curIndex];
  }

  private void UpdateVelocity()
  {
    _velocity = _posHistory[_posHistoryIndex] - PreviousPosition();
  }

  /// <summary>
  /// Update Grounded, if player is/isn't grounded.
  /// Copied from the Unity Essentials Project.
  /// </summary>
  private void GroundedCheck()
  {
    // set sphere position, with offset
    //Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y
    //  + GroundedOffset, transform.position.z);
    // True if on anything tagged GroundLayers
    //Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
    //    QueryTriggerInteraction.Ignore);
  }

  public void Move(Vector3 input)
  {
    _velocity = input;
    transform.position += input;
    UpdatePositionHistory();
  }
}
