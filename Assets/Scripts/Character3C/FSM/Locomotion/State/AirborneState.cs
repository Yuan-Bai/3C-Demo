
public class AirborneState : LocomotionStateBase
{
    private const float JumpDuration = 0.18f;
    private const float JumpSecondDuration = 0.18f;

    public AirborneState(LocomotionStateId id, StateMachine<LocomotionStateId> stateMachine, LocomotionContext context) : base(id, stateMachine, context)
    {
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);
        
        if (Context.IsStableOnGround)
        {
            ChangeState(LocomotionStateId.Grounded);
            return;
        }

        if (InputFrame.DashPressed)
        {
            StartAirborneAction(AirborneActionId.JumpSecond);
        }

        if (Context.AirborneAction != AirborneActionId.None)
        {
            TickAirborneAction(deltaTime);
            return;
        }
    }

    public override void Exit()
    {
        base.Exit();
        ClearAirborneAction();
    }

    private void StartAirborneAction(AirborneActionId actionId)
    {
        Context.AirborneAction = actionId;
        Context.AirborneActionElapsedTime = 0.0f;
    }

    private void TickAirborneAction(float deltaTime)
    {
        Context.AirborneActionElapsedTime += deltaTime;

        switch (Context.AirborneAction)
        {
            case AirborneActionId.Jump:
                TickJumpAction();
                break;
            case AirborneActionId.JumpSecond:
                TickJumpSecondAction();
                break;
            case AirborneActionId.None:
            default:
                ClearAirborneAction();
                TickAirborneGait();
                break;
        }
    }

    private void TickJumpAction()
    {
        if (Context.AirborneActionElapsedTime < JumpDuration)
        {
            return;
        }

        ClearAirborneAction();
        TickAirborneGait();
    }

    private void TickJumpSecondAction()
    {
        if (Context.AirborneActionElapsedTime < JumpSecondDuration)
        {
            return;
        }

        ClearAirborneAction();
        TickAirborneGait();
    }

    private void ClearAirborneAction()
    {
        Context.AirborneAction = AirborneActionId.None;
        Context.AirborneActionElapsedTime = 0.0f;
    }

    private void TickAirborneGait()
    {
        Context.AirbornePhase = AirbornePhase.Falling;
    }
}