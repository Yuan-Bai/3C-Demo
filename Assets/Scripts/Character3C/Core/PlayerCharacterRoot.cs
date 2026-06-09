using UnityEngine;

public class PlayerCharacterRoot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KccCharacterController _controller;

    [Header("Debug")]
    [SerializeField] private LocomotionStateId _debugLocomotionState;

    private readonly StateMachine<LocomotionStateId> _locomotionStateMachine = new();
    private readonly LocomotionContext _locomotionContext = new();

    private void Awake()
    {
        _controller ??= GetComponent<KccCharacterController>();

        if (_controller != null)
        {
            _controller.SetLocomotionContext(_locomotionContext);
        }

        _locomotionStateMachine.AddState(new GroundedState(LocomotionStateId.Grounded, _locomotionStateMachine, _locomotionContext));
        _locomotionStateMachine.AddState(new AirborneState(LocomotionStateId.Airborne, _locomotionStateMachine, _locomotionContext));
        _locomotionStateMachine.AddState(new ClimbingState(LocomotionStateId.Climbing, _locomotionStateMachine, _locomotionContext));
        _locomotionStateMachine.AddState(new LandingState(LocomotionStateId.Landing, _locomotionStateMachine, _locomotionContext));

        _locomotionStateMachine.ChangeState(LocomotionStateId.Grounded);
        _debugLocomotionState = _locomotionStateMachine.CurrentStateId;
    }

    private void OnEnable()
    {
        if (_controller != null)
        {
            _controller.MotorUpdated += OnMotorUpdated;
        }
    }

    private void OnDisable()
    {
        if (_controller != null)
        {
            _controller.MotorUpdated -= OnMotorUpdated;
        }
    }

    private void OnMotorUpdated(float deltaTime)
    {
        _locomotionStateMachine.Tick(deltaTime);
        _debugLocomotionState = _locomotionStateMachine.CurrentStateId;
    }
}
