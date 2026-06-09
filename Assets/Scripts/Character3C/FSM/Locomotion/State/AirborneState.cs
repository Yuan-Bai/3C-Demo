
public class AirborneState : LocomotionStateBase
{
    public AirborneState(LocomotionStateId id, StateMachine<LocomotionStateId> stateMachine, LocomotionContext context) : base(id, stateMachine, context)
    {
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);
        if (Context.IsStableOnGround)
        {
            ChangeState(LocomotionStateId.Landing);
        }
    }
}