using Animancer;
using UnityEngine;

public delegate void PlayIfChanged(CharacterAnimationKey key, ClipTransition transition, bool notifyEnd=false);

[RequireComponent(typeof(KccCharacterController))]
[RequireComponent(typeof(CharacterAnimancerController))]
public class PlayerCharacterRoot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KccCharacterController _kccController;
    [SerializeField] private CharacterAnimancerController _animancerController;
    [SerializeField] private ICharacterAnimationDriver _animation;

    [Header("Debug")]
    [SerializeField] private LocomotionStateId _debugLocomotionState;
    [SerializeField] private GroundedStateId _debugGroundedStateId;
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
        _animation ??= GetComponent<CharacterAnimancerController>();
        
        _kccController.SetLocomotionContext(_locomotionContext);
        _animancerController.SetLocomotionContext(_locomotionContext);

        _locomotionStateMachine.AddState(new GroundedState(LocomotionStateId.Grounded, _locomotionStateMachine, _locomotionContext, _animation));
        _locomotionStateMachine.AddState(new AirborneState(LocomotionStateId.Airborne, _locomotionStateMachine, _locomotionContext, _animation));
        _locomotionStateMachine.AddState(new ClimbingState(LocomotionStateId.Climbing, _locomotionStateMachine, _locomotionContext, _animation));

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

        if (_animancerController != null)
        {
            _animancerController.AnimationEnded += OnAnimationEnded;
        }
    }

    private void OnDisable()
    {
        if (_kccController != null)
        {
            _kccController.MotorBeforeUpdated -= OnMotorBeforeUpdated;
            _kccController.MotorUpdated -= OnMotorUpdated;
        }

        if (_animancerController != null)
        {
            _animancerController.AnimationEnded -= OnAnimationEnded;
        }
    }

    private void OnMotorBeforeUpdated(float deltaTime)
    {
        _locomotionStateMachine.Tick(deltaTime);
        _debugLocomotionState = _locomotionStateMachine.CurrentStateId;

        _animancerController?.UpdateAnimation(_locomotionStateMachine.CurrentStateId);
    }

    private void OnAnimationEnded(CharacterAnimationKey key)
    {
        if (_locomotionStateMachine.CurrentState is IAnimationEventReceiver receiver)
        {
            receiver.OnAnimationEnded(key);
        }
    }

    private void OnMotorUpdated(float deltaTime)
    {
        _debugLocomotionState = _locomotionStateMachine.CurrentStateId;
        _debugGroundedStateId = _locomotionContext.GroundedStateId;
        _debugGroundedActionElapsedTime = _locomotionContext.GroundedActionElapsedTime;
        _debugGroundedDashHeldTime = _locomotionContext.GroundedDashHeldTime;
        _debugAirbornePhase = _locomotionContext.AirbornePhase;
        _debugAirborneAction = _locomotionContext.AirborneAction;
        _debugAirborneActionElapsedTime = _locomotionContext.AirborneActionElapsedTime;
    }
}
