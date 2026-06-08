public class LandingState : LocomotionStateBase
{
    // 临时测试变量
    private float temp = 1.0f;

    public LandingState(LocomotionStateId id, StateMachine<LocomotionStateId> stateMachine, Context context) : base(id, stateMachine, context)
    {
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);
        // temp*
        temp -= deltaTime;
        if (temp <= 0)
        {
            ChangeState(LocomotionStateId.Grounded);
        }
    }
}