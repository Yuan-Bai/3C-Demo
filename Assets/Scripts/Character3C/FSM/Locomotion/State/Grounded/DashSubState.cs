


public class DashSubState : IState<GroundedStateId>, IAnimationEventReceiver
{
    public GroundedStateId Id => GroundedStateId.Dash;

    private LocomotionContext _context;
    private GroundedStateContext _groundedContext;
    private ChangeSubState ChangeSubState;
    private ICharacterAnimationDriver _animation;

    public DashSubState(LocomotionContext context, GroundedStateContext groundedContext, ChangeSubState changeSubState, ICharacterAnimationDriver animation)
    {
        _context = context;
        _groundedContext = groundedContext;
        ChangeSubState = changeSubState;
        _animation = animation;
    }

    public void Enter()
    {
        _context.GroundedStateId = GroundedStateId.Dash;
        _context.GroundedActionElapsedTime = 0.0f;
        _groundedContext.DashHeldTime = 0.0f;
        _context.UseRootMotion = true;
        _animation.Play(new AnimationCommand(CharacterAnimationKey.DashF, true, true, 0.2f, false));
    }

    public void Exit()
    {
        _context.UseRootMotion = false;
    }

    public void Tick(float deltaTime)
    {
        _context.GroundedActionElapsedTime += deltaTime;
    }

    public void OnAnimationEnded(CharacterAnimationKey key)
    {

        if (_context.HasMoveInput)
        {
            ChangeSubState(GroundedStateId.SprintImpulse);
            return;
        }

        ChangeSubState(GroundedStateId.Idle);
    }
}
