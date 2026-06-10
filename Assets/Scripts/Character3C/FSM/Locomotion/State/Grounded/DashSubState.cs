


public class DashSubState : IState<GroundedStateId>, IAnimationEventReceiver
{
    public GroundedStateId Id => GroundedStateId.Dash;

    private LocomotionContext _context;
    private GroundedStateContext _groundedContext;
    private ChangeSubState ChangeSubState;

    public DashSubState(LocomotionContext context, GroundedStateContext groundedContext, ChangeSubState changeSubState)
    {
        _context = context;
        _groundedContext = groundedContext;
        ChangeSubState = changeSubState;
    }

    public void Enter()
    {
        _context.GroundedStateId = GroundedStateId.Dash;
        _context.GroundedActionElapsedTime = 0.0f;
        _groundedContext.DashHeldTime = 0.0f;
    }

    public void Exit()
    {
    }

    public void Tick(float deltaTime)
    {
        _context.GroundedActionElapsedTime += deltaTime;
    }

    public void OnAnimationEnded(CharacterAnimationKey key)
    {
        if (key != CharacterAnimationKey.DashF && key != CharacterAnimationKey.DashB)
        {
            return;
        }

        if (_context.HasMoveInput)
        {
            ChangeSubState(GroundedStateId.SprintImpulse);
            return;
        }

        ChangeSubState(GroundedStateId.Idle);
    }
}
