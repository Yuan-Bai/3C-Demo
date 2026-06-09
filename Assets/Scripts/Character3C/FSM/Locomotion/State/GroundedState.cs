
public class GroundedState : LocomotionStateBase
{
    public GroundedState(LocomotionStateId id, StateMachine<LocomotionStateId> stateMachine, LocomotionContext context) : base(id, stateMachine, context)
    {
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        // 大状态切换
        if (!Context.IsStableOnGround)
        {
            ChangeState(LocomotionStateId.Airborne);
            return;
        }

        if (InputFrame.JumpPressed)
        {
            ChangeState(LocomotionStateId.Airborne);
            return;
        }

        if (Context.GroundedGait == GroundedGait.Idle)
        {
            IdleTick();
        }
        else if (Context.GroundedGait == GroundedGait.Walk)
        {
            WalkTick();
        }
        else if (Context.GroundedGait == GroundedGait.Run)
        {
            RunTick();
        }
        else if (Context.GroundedGait == GroundedGait.Sprint)
        {
            SprintTick();
        }
    }

    private void IdleTick()
    {
        if (Context.HasMoveInput)
        {
            if (InputFrame.MoveAxis.magnitude <= 0.5f)
            {
                Context.GroundedGait = GroundedGait.Walk;
            }
            else
            {
                Context.GroundedGait = GroundedGait.Run;
            }
        }
    }

    private void WalkTick()
    {
        if (!Context.HasMoveInput)
        {
            Context.GroundedGait = GroundedGait.Idle;
        }
    }

    private void RunTick()
    {
        if (!Context.HasMoveInput)
        {
            Context.GroundedGait = GroundedGait.Idle;
        }
    }

    private void SprintTick()
    {
        
    }
}