
public class GroundedState : LocomotionStateBase
{
    private const float SprintHoldThreshold = 0.5f;
    private const float DashDuration = 0.18f;
    private const float SprintImpulseDuration = 0.22f;
    private const float MoveStopDuration = 0.12f;

    private bool _preferWalk;

    public GroundedState(LocomotionStateId id, StateMachine<LocomotionStateId> stateMachine, LocomotionContext context) : base(id, stateMachine, context)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Context.GroundedAction = GroundedActionId.None;
        Context.GroundedActionElapsedTime = 0.0f;
        Context.GroundedDashHeldTime = 0.0f;
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if (!Context.IsStableOnGround)
        {
            ClearGroundedAction();
            ChangeState(LocomotionStateId.Airborne);
            return;
        }

        if (InputFrame.JumpPressed)
        {
            // StartGroundedAction(GroundedActionId.JumpStart);
            StartAirborneAction(AirborneActionId.Jump);
            ChangeState(LocomotionStateId.Airborne);
            return;
        }

        if (InputFrame.MoveSwitchPressed)
        {
            _preferWalk = !_preferWalk;
        }

        if (InputFrame.DashPressed && CanStartDash())
        {
            StartGroundedAction(GroundedActionId.Dash);
        }

        TickDashHold(deltaTime);

        if (Context.GroundedAction != GroundedActionId.None)
        {
            TickGroundedAction(deltaTime);
            return;
        }

        TickGroundedGait();
    }

    private void TickGroundedGait()
    {
        if (!Context.HasMoveInput)
        {
            if (IsMovingGait(Context.GroundedGait))
            {
                StartGroundedAction(GroundedActionId.MoveStop);
                return;
            }

            Context.GroundedGait = GroundedGait.Idle;
            return;
        }

        if (Context.GroundedGait == GroundedGait.Sprint)
        {
            if (InputFrame.DashHeld)
            {
                return;
            }

            Context.GroundedGait = GetPreferredMovingGait();
            return;
        }

        Context.GroundedGait = GetPreferredMovingGait();
    }

    private void TickGroundedAction(float deltaTime)
    {
        Context.GroundedActionElapsedTime += deltaTime;

        switch (Context.GroundedAction)
        {
            case GroundedActionId.Dash:
                TickDashAction();
                break;
            case GroundedActionId.SprintImpulse:
                TickSprintImpulseAction();
                break;
            case GroundedActionId.MoveStop:
                TickMoveStopAction();
                break;
            case GroundedActionId.JumpStart:
            case GroundedActionId.None:
            default:
                ClearGroundedAction();
                TickGroundedGait();
                break;
        }
    }

    private void TickDashAction()
    {
        if (Context.GroundedActionElapsedTime < DashDuration)
        {
            return;
        }

        if (Context.HasMoveInput)
        {
            StartGroundedAction(GroundedActionId.SprintImpulse);
            return;
        }

        ClearGroundedAction();
        TickGroundedGait();
    }

    private void TickSprintImpulseAction()
    {
        if (!Context.HasMoveInput)
        {
            StartGroundedAction(GroundedActionId.MoveStop);
            return;
        }

        if (Context.GroundedActionElapsedTime < SprintImpulseDuration)
        {
            return;
        }

        if (InputFrame.DashHeld && Context.GroundedDashHeldTime >= SprintHoldThreshold)
        {
            ClearGroundedAction();
            Context.GroundedGait = GroundedGait.Sprint;
            return;
        }

        ClearGroundedAction();
        TickGroundedGait();
    }

    private void TickMoveStopAction()
    {
        if (Context.HasMoveInput)
        {
            ClearGroundedAction();
            TickGroundedGait();
            return;
        }

        if (Context.GroundedActionElapsedTime < MoveStopDuration)
        {
            return;
        }

        ClearGroundedAction();
        Context.GroundedGait = GroundedGait.Idle;
    }

    private void StartGroundedAction(GroundedActionId actionId)
    {
        Context.GroundedAction = actionId;
        Context.GroundedActionElapsedTime = 0.0f;

        if (actionId == GroundedActionId.Dash)
        {
            Context.GroundedDashHeldTime = 0.0f;
        }
    }

    private void StartAirborneAction(AirborneActionId actionId)
    {
        Context.AirborneAction = actionId;
        Context.AirborneActionElapsedTime = 0.0f;
    }

    private void ClearGroundedAction()
    {
        Context.GroundedAction = GroundedActionId.None;
        Context.GroundedActionElapsedTime = 0.0f;

        if (!InputFrame.DashHeld)
        {
            Context.GroundedDashHeldTime = 0.0f;
        }
    }

    private void TickDashHold(float deltaTime)
    {
        if (InputFrame.DashHeld)
        {
            Context.GroundedDashHeldTime += deltaTime;
            return;
        }

        if (Context.GroundedAction != GroundedActionId.Dash && Context.GroundedAction != GroundedActionId.SprintImpulse)
        {
            Context.GroundedDashHeldTime = 0.0f;
        }
    }

    private GroundedGait GetPreferredMovingGait()
    {
        return _preferWalk ? GroundedGait.Walk : GroundedGait.Run;
    }

    private static bool IsMovingGait(GroundedGait gait)
    {
        return gait == GroundedGait.Walk || gait == GroundedGait.Run || gait == GroundedGait.Sprint;
    }

    private bool CanStartDash()
    {
        return Context.GroundedAction == GroundedActionId.None || Context.GroundedAction == GroundedActionId.MoveStop;
    }
}
