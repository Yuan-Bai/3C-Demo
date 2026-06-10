using System;
using KinematicCharacterController;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class KccCharacterController : MonoBehaviour, ICharacterController
{
    [SerializeField] private float _rotationSharpness = 12f;
    [SerializeField] private float _moveSpeedSharpness = 12f;
    [SerializeField] private float _maxStableMoveSpeed = 5f;
    [SerializeField] private float _jumpSpeed = 10f;
    [SerializeField] private float _secondJumpSpeed = 8f;
    [SerializeField] private float _gravity = 12f;
    [SerializeField] private Vector3 _gravityDir = Vector3.down;

    private KinematicCharacterMotor _motor;
    private PlayerInputReader _inputReader;
    private Transform _camTransform;
    private CharacterInputFrame _inputFrame;
    private Vector3 _moveDirection;
    private Vector3 _lookDirection;
    private LocomotionContext _locomotionContext;
    private AirborneActionId _lastAppliedAirborneAction;

    public event Action<float> MotorBeforeUpdated;
    public event Action<float> MotorUpdated;

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

    public void SetLocomotionContext(LocomotionContext locomotionContext)
    {
        _locomotionContext = locomotionContext;
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        if (_motor == null || _locomotionContext == null)
        {
            return;
        }

        WriteLocomotionContext();
        MotorUpdated?.Invoke(deltaTime);
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _moveDirection = Vector3.zero;
        _lookDirection = Vector3.zero;
        _inputFrame = _inputReader == null ? CharacterInputFrame.Empty : _inputReader.ConsumeFrame();

        if (_motor == null || _camTransform == null)
        {
            WriteLocomotionContext();
            MotorBeforeUpdated?.Invoke(deltaTime);
            return;
        }

        Vector3 characterUp = _motor.CharacterUp;
        Vector3 forward = Vector3.ProjectOnPlane(_camTransform.forward, characterUp);
        if (forward.magnitude < 1e-6)
        {
            forward = Vector3.ProjectOnPlane(_camTransform.up, characterUp);
        }

        if (forward.magnitude < 1e-6)
        {
            WriteLocomotionContext();
            MotorBeforeUpdated?.Invoke(deltaTime);
            return;
        }

        forward = forward.normalized;
        _lookDirection = forward;

        Vector2 moveAxis = Vector2.ClampMagnitude(_inputFrame.MoveAxis, 1.0f);
        if (moveAxis.magnitude < 1e-6)
        {
            WriteLocomotionContext();
            MotorBeforeUpdated?.Invoke(deltaTime);
            return;
        }

        _moveDirection = Quaternion.LookRotation(forward, characterUp) * new Vector3(moveAxis.x, 0.0f, moveAxis.y);
        WriteLocomotionContext();
        MotorBeforeUpdated?.Invoke(deltaTime);
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

    public void ProcessHitStabilityReport(
        Collider hitCollider,
        Vector3 hitNormal,
        Vector3 hitPoint,
        Vector3 atCharacterPosition,
        Quaternion atCharacterRotation,
        ref HitStabilityReport hitStabilityReport)
    {
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_motor == null || _moveDirection.magnitude < 1e-6)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection.normalized, _motor.CharacterUp);
        float blend = 1.0f - Mathf.Exp(-_rotationSharpness * deltaTime);
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, blend);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (_motor == null)
        {
            return;
        }

        if (_locomotionContext != null && _locomotionContext.AirborneAction == AirborneActionId.None)
        {
            _lastAppliedAirborneAction = AirborneActionId.None;
        }

        if (_motor.GroundingStatus.IsStableOnGround)
        {
            if (TryApplyAirborneActionImpulse(ref currentVelocity))
            {
                return;
            }

            float speedMultiplier = _locomotionContext.GroundedGait switch
            {
                GroundedGait.Idle => 0f,
                GroundedGait.Walk => 0.5f,
                GroundedGait.Run => 1f,
                GroundedGait.Sprint => 1.3f,
                _ => 1f,
            };
            Vector3 targetVelocity = _moveDirection * _maxStableMoveSpeed * speedMultiplier;

            currentVelocity = _motor.GetDirectionTangentToSurface(currentVelocity, _motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
            
            float blend = 1.0f - Mathf.Exp(-_moveSpeedSharpness * deltaTime);
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, blend);
        }
        else
        {
            TryApplyAirborneActionImpulse(ref currentVelocity);
            currentVelocity += _gravityDir * _gravity * deltaTime;
        }
    }

    private void WriteLocomotionContext()
    {
        if (_motor == null || _locomotionContext == null)
        {
            return;
        }

        _locomotionContext.IsStableOnGround = _motor.GroundingStatus.IsStableOnGround;
        _locomotionContext.HasMoveInput = _moveDirection.magnitude >= 1e-6;
        _locomotionContext.HorizontalSpeed = new Vector3(_motor.Velocity.x, 0.0f, _motor.Velocity.z);
        _locomotionContext.VerticalSpeed = _motor.Velocity.y;
        _locomotionContext.MoveDirection = _moveDirection;
        _locomotionContext.LookDirection = _lookDirection;
        _locomotionContext.InputFrame = _inputFrame;
    }

    private bool TryApplyAirborneActionImpulse(ref Vector3 currentVelocity)
    {
        if (_locomotionContext == null ||
            _locomotionContext.AirborneAction == AirborneActionId.None ||
            _locomotionContext.AirborneAction == _lastAppliedAirborneAction)
        {
            return false;
        }

        float impulseSpeed = _locomotionContext.AirborneAction switch
        {
            AirborneActionId.Jump => _jumpSpeed,
            AirborneActionId.JumpSecond => _secondJumpSpeed,
            _ => 0.0f,
        };

        if (impulseSpeed <= 0.0f)
        {
            return false;
        }

        Vector3 verticalVelocity = Vector3.Project(currentVelocity, _motor.CharacterUp);
        currentVelocity = currentVelocity - verticalVelocity + _motor.CharacterUp * impulseSpeed;
        _motor.ForceUnground();
        _lastAppliedAirborneAction = _locomotionContext.AirborneAction;
        return true;
    }
}
