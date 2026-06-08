using KinematicCharacterController;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class KccCharacterController : MonoBehaviour, ICharacterController
{
    [SerializeField] private float _rotationSharpness = 12f;
    [SerializeField] private float _moveSpeedSharpness = 12f;
    [SerializeField] private float _maxStableMoveSpeed = 5f;
    [SerializeField] private float _gravity = 12f;
    [SerializeField] private Vector3 _gravityDir = Vector3.down;
    private KinematicCharacterMotor _motor;
    private PlayerInputReader _inputReader;
    private Transform _camTransform;
    private CharacterInputFrame _inputFrame;
    private Vector3 _moveDirection;
    private Vector3 _lookDirection;
    public Context context;

    private void Awake()
    {
        _motor ??= GetComponent<KinematicCharacterMotor>();
        _inputReader ??= GetComponent<PlayerInputReader>();

        if (_motor != null)
        {
            _motor.CharacterController = this;
        }
    }

    private void Start()
    {
        _camTransform = Camera.main == null ? null : Camera.main.transform;
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        if (context == null) Debug.Log("context");
        // if (_motor.GroundingStatus) Debug.Log("_motor.GroundingStatus");
        context.IsStableOnGround = _motor.GroundingStatus.IsStableOnGround;
        context.HasMoveInput = _moveDirection.magnitude >= 1e-6;
        context.HorizontalSpeed = new Vector3(_motor.Velocity.x, 0.0f, _motor.Velocity.z);
        context.VerticalSpeed = _motor.Velocity.y;
        context.MoveDirection = _moveDirection;
        context.LookDirection = _lookDirection;
        context.InputFrame = _inputFrame;
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _moveDirection = Vector3.zero;
        if (_motor == null)
        {
            return;
        }
        Vector3 characterUp = _motor.CharacterUp;
        Vector3 forward = Vector3.ProjectOnPlane(_camTransform.forward, characterUp);
        if (forward.magnitude < 1e-6)
        {
            forward = Vector3.ProjectOnPlane(_camTransform.up, characterUp);
        }
        forward = forward.normalized;
        _lookDirection = forward;

        _inputFrame = _inputReader == null ? CharacterInputFrame.Empty : _inputReader.ConsumeFrame();
        Vector2 moveAxis = Vector2.ClampMagnitude(_inputFrame.MoveAxis, 1.0f);
        if (moveAxis.magnitude < 1e-6 || _camTransform == null)
        {
            return;
        }
        _moveDirection = Quaternion.LookRotation(forward, characterUp) * new Vector3(moveAxis.x, 0.0f, moveAxis.y);
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void PostGroundingUpdate(float deltaTime)
    {
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_moveDirection.magnitude < 1e-6)
        {
            return;
        }
        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection.normalized, _motor.CharacterUp);
        float blend = 1.0f - Mathf.Exp(-_rotationSharpness * deltaTime);
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, blend);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (_motor.GroundingStatus.IsStableOnGround)
        {
            // 角色的CharacterUp不一定和地面法线方向一致，比如上坡
            currentVelocity = _motor.GetDirectionTangentToSurface(currentVelocity, _motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

            Vector3 targetVelocity = _moveDirection * _maxStableMoveSpeed;

            float blend = 1.0f - Mathf.Exp(-_moveSpeedSharpness*deltaTime);
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, blend);
        }
        else
        {
            currentVelocity += _gravityDir*_gravity*deltaTime;
        }
    }
}
