


public class RunSubState : IState<GroundedStateId>
{
    public GroundedStateId Id => GroundedStateId.Run;

    private LocomotionContext _context;
    private GroundedStateContext _groundedContext;
    private ChangeSubState ChangeSubState;

    public RunSubState(LocomotionContext context, GroundedStateContext groundedContext, ChangeSubState changeSubState)
    {
        _context = context;
        _groundedContext = groundedContext;
        ChangeSubState = changeSubState;
    }

    public void Enter()
    {
        _context.GroundedStateId = GroundedStateId.Run;
    }

    public void Exit()
    {
    }

    public void Tick(float deltaTime)
    {
        if (!_context.HasMoveInput)
        {
            ChangeSubState(GroundedStateId.MoveStop);
            return;
        }

        if (_groundedContext.PreferWalk)
        {
            ChangeSubState(GroundedStateId.Walk);
            return;
        }

        if (_context.InputFrame.DashPressed)
        {
            ChangeSubState(GroundedStateId.Dash);
        }
    }
}