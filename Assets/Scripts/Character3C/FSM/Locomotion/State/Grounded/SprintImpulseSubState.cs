


public class SprintImpulseSubState : IState<GroundedStateId>, IAnimationEventReceiver
{
    private const float SprintHoldThreshold = 0.5f;

    public GroundedStateId Id => GroundedStateId.SprintImpulse;

    private LocomotionContext _context;
    private GroundedStateContext _groundedContext;
    private ChangeSubState ChangeSubState;

    public SprintImpulseSubState(LocomotionContext context, GroundedStateContext groundedContext, ChangeSubState changeSubState)
    {
        _context = context;
        _groundedContext = groundedContext;
        ChangeSubState = changeSubState;
    }

    public void Enter()
    {
        _context.GroundedStateId = GroundedStateId.SprintImpulse;
        _context.GroundedActionElapsedTime = 0.0f;
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
        if (key != CharacterAnimationKey.SprintImpulse)
        {
            return;
        }

        if (!_context.HasMoveInput)
        {
            ChangeSubState(GroundedStateId.MoveStop);
            return;
        }

        if (_context.InputFrame.DashHeld && _groundedContext.DashHeldTime >= SprintHoldThreshold)
        {
            ChangeSubState(GroundedStateId.Sprint);
            return;
        }

        ChangeSubState(_groundedContext.PreferWalk ? GroundedStateId.Walk : GroundedStateId.Run);
    }
}
