public delegate bool ChangeSubState(GroundedStateId nextSubStateId);

public class GroundedState : LocomotionStateBase, IAnimationEventReceiver
{
    private readonly GroundedStateContext _groundedContext = new();

    private readonly StateMachine<GroundedStateId> _subStateMachine = new();

    private ICharacterAnimationDriver _animation;

    public GroundedState(LocomotionStateId id, StateMachine<LocomotionStateId> stateMachine, LocomotionContext context, ICharacterAnimationDriver animation) : base(id, stateMachine, context, animation)
    {
        _subStateMachine.AddState(new MoveSubState(GroundedStateId.Idle, Context, _groundedContext, ChangeGroundedSubState));
        _subStateMachine.AddState(new MoveSubState(GroundedStateId.Walk, Context, _groundedContext, ChangeGroundedSubState));
        _subStateMachine.AddState(new MoveSubState(GroundedStateId.Run, Context, _groundedContext, ChangeGroundedSubState));
        _subStateMachine.AddState(new MoveSubState(GroundedStateId.Sprint, Context, _groundedContext, ChangeGroundedSubState));
        _subStateMachine.AddState(new SprintImpulseSubState(Context, _groundedContext, ChangeGroundedSubState, animation));
        _subStateMachine.AddState(new DashSubState(Context, _groundedContext, ChangeGroundedSubState, animation));
        _subStateMachine.AddState(new MoveStopSubState(Context, _groundedContext, ChangeGroundedSubState, animation));

        _animation = animation;
    }

    public override void Enter()
    {
        base.Enter();
        Context.GroundedActionElapsedTime = 0.0f;
        Context.GroundedDashHeldTime = 0.0f;
        _groundedContext.DashHeldTime = 0.0f;

        if (!Context.HasMoveInput)
        {
            ChangeGroundedSubState(GroundedStateId.Idle);
        }
        else if (_groundedContext.PreferWalk)
        {
            ChangeGroundedSubState(GroundedStateId.Walk);
        }
        else if (!_groundedContext.PreferWalk)
        {
            ChangeGroundedSubState(GroundedStateId.Run);
        }
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if (!Context.IsStableOnGround)
        {
            ChangeState(LocomotionStateId.Airborne);
            return;
        }

        if (InputFrame.JumpPressed)
        {
            Context.AirborneAction = AirborneActionId.Jump;
            Context.AirborneActionElapsedTime = 0.0f;
            ChangeState(LocomotionStateId.Airborne);
            return;
        }

        if (InputFrame.MoveSwitchPressed)
        {
            _groundedContext.PreferWalk = !_groundedContext.PreferWalk;
        }

        TickDashHeldTime(deltaTime);

        _subStateMachine.Tick(deltaTime);
    }

    public void OnAnimationEnded(CharacterAnimationKey key)
    {
        if (_subStateMachine.CurrentState is IAnimationEventReceiver receiver)
        {
            receiver.OnAnimationEnded(key);
        }
    }

    private bool ChangeGroundedSubState(GroundedStateId nextSubStateId)
    {
        GroundedStateId previousStateId = _subStateMachine.CurrentStateId;
        bool changed = _subStateMachine.ChangeState(nextSubStateId);
        if (changed)
        {
            _groundedContext.PreviousSubStateId = previousStateId;

            if (IsMoveState(nextSubStateId))
            {
                _animation.Play(new AnimationCommand(
                    CharacterAnimationKey.Move,
                    false,
                    false,
                    0.2f,
                    false));
            }
        }

        return changed;
    }

    private bool IsMoveState(GroundedStateId nextSubStateId)
    {
        return nextSubStateId == GroundedStateId.Idle
        || nextSubStateId == GroundedStateId.Walk
        || nextSubStateId == GroundedStateId.Run
        || nextSubStateId == GroundedStateId.Sprint;
    }

    private void TickDashHeldTime(float deltaTime)
    {
        if (InputFrame.DashHeld)
        {
            _groundedContext.DashHeldTime += deltaTime;
        }
        else
        {
            _groundedContext.DashHeldTime = 0.0f;
        }

        Context.GroundedDashHeldTime = _groundedContext.DashHeldTime;
    }
}
