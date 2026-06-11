
public class MoveSubState : IState<GroundedStateId>
{
    public GroundedStateId Id { get; }

    private LocomotionContext _context;
    private GroundedStateContext _groundedContext;
    private ChangeSubState ChangeSubState;

    public MoveSubState(GroundedStateId id, LocomotionContext context, GroundedStateContext groundedContext, ChangeSubState changeSubState)
    {
        Id = id;
        _context = context;
        _groundedContext = groundedContext;
        ChangeSubState = changeSubState;
    }

    public void Enter()
    {
        _context.GroundedStateId = Id;
    }

    public void Exit()
    {
    }

    public void Tick(float deltaTime)
    {
        if (_context.InputFrame.DashPressed)
        {
            ChangeSubState(GroundedStateId.Dash);
            return;
        }

        switch (Id)
        {
            case GroundedStateId.Idle:
                if (_context.HasMoveInput)
                {
                    if (_groundedContext.PreferWalk)
                    {
                        ChangeSubState(GroundedStateId.Walk);
                        return;
                    }
                    else
                    {
                        ChangeSubState(GroundedStateId.Run);
                        return;
                    }
                }
                break;
            case GroundedStateId.Walk:
                if (!_context.HasMoveInput)
                {
                    ChangeSubState(GroundedStateId.MoveStop);
                    return;
                }

                if (!_groundedContext.PreferWalk)
                {
                    ChangeSubState(GroundedStateId.Run);
                    return;
                }
                break;
            case GroundedStateId.Run:
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
                break;
            case GroundedStateId.Sprint:
                if (!_context.HasMoveInput)
                {
                    ChangeSubState(GroundedStateId.MoveStop);
                    return;
                }
                break;
            default:
            break;
        }
    }
}