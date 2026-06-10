using UnityEngine;

public class PlayerCharacterRoot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KccCharacterController _kccController;
    [SerializeField] private CharacterAnimancerController _animancerController;

    [Header("Debug")]
    [SerializeField] private LocomotionStateId _debugLocomotionState;
    [SerializeField] private GroundedGait _debugGroundedGait;
    [SerializeField] private GroundedActionId _debugGroundedAction;
    [SerializeField] private float _debugGroundedActionElapsedTime;
    [SerializeField] private float _debugGroundedDashHeldTime;
    [SerializeField] private AirbornePhase _debugAirbornePhase;
    [SerializeField] private AirborneActionId _debugAirborneAction;
    [SerializeField] private float _debugAirborneActionElapsedTime;

    private readonly StateMachine<LocomotionStateId> _locomotionStateMachine = new();
    private readonly LocomotionContext _locomotionContext = new();

    private void Awake()
    {
        _kccController ??= GetComponent<KccCharacterController>();
        _animancerController ??= GetComponent<CharacterAnimancerController>();
        if (_kccController != null)
        {
            _kccController.SetLocomotionContext(_locomotionContext);
        }

        _locomotionStateMachine.AddState(new GroundedState(LocomotionStateId.Grounded, _locomotionStateMachine, _locomotionContext));
        _locomotionStateMachine.AddState(new AirborneState(LocomotionStateId.Airborne, _locomotionStateMachine, _locomotionContext));
        _locomotionStateMachine.AddState(new ClimbingState(LocomotionStateId.Climbing, _locomotionStateMachine, _locomotionContext));

        _locomotionStateMachine.ChangeState(LocomotionStateId.Grounded);
        _debugLocomotionState = _locomotionStateMachine.CurrentStateId;
    }

    private void OnEnable()
    {
        if (_kccController != null)
        {
            _kccController.MotorBeforeUpdated += OnMotorBeforeUpdated;
            _kccController.MotorUpdated += OnMotorUpdated;
        }
    }

    private void OnDisable()
    {
        if (_kccController != null)
        {
            _kccController.MotorBeforeUpdated -= OnMotorBeforeUpdated;
            _kccController.MotorUpdated -= OnMotorUpdated;
        }
    }

    private void OnMotorBeforeUpdated(float deltaTime)
    {
        _locomotionStateMachine.Tick(deltaTime);
        _debugLocomotionState = _locomotionStateMachine.CurrentStateId;
    }

    private void OnMotorUpdated(float deltaTime)
    {
        _debugLocomotionState = _locomotionStateMachine.CurrentStateId;
        _debugGroundedGait = _locomotionContext.GroundedGait;
        _debugGroundedAction = _locomotionContext.GroundedAction;
        _debugGroundedActionElapsedTime = _locomotionContext.GroundedActionElapsedTime;
        _debugGroundedDashHeldTime = _locomotionContext.GroundedDashHeldTime;
        _debugAirbornePhase = _locomotionContext.AirbornePhase;
        _debugAirborneAction = _locomotionContext.AirborneAction;
        _debugAirborneActionElapsedTime = _locomotionContext.AirborneActionElapsedTime;
    }
}
