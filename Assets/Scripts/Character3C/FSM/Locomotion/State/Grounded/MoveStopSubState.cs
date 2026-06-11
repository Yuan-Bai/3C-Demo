



using UnityEngine;

public class MoveStopSubState : IState<GroundedStateId>, IAnimationEventReceiver
{
    public GroundedStateId Id => GroundedStateId.MoveStop;

    private LocomotionContext _context;
    private GroundedStateContext _groundedContext;
    private ChangeSubState ChangeSubState;
    private ICharacterAnimationDriver _animation;

    public MoveStopSubState(LocomotionContext context, GroundedStateContext groundedContext, ChangeSubState changeSubState, ICharacterAnimationDriver animation)
    {
        _context = context;
        _groundedContext = groundedContext;
        ChangeSubState = changeSubState;
        _animation = animation;
    }

    public void Enter()
    {
        _context.GroundedStateId = GroundedStateId.MoveStop;
        _context.GroundedActionElapsedTime = 0.0f;
        _context.UseRootMotion = true;
        _animation.Play(new AnimationCommand(CharacterAnimationKey.StopRunL, true, true, 0.2f, false));
    }

    public void Exit()
    {
        _context.UseRootMotion = false;
    }

    public void Tick(float deltaTime)
    {
        _context.GroundedActionElapsedTime += deltaTime;

        if (_context.HasMoveInput)
        {
            if (_groundedContext.PreferWalk)
            {
                ChangeSubState(GroundedStateId.Walk);
            }
            else
            {
                ChangeSubState(GroundedStateId.Run);
            }
            return;
        }

        if (_context.InputFrame.DashPressed)
        {
            ChangeSubState(GroundedStateId.Dash);
            return;
        }
    }

    public void OnAnimationEnded(CharacterAnimationKey key)
    {
        if (key != CharacterAnimationKey.StopWalkL &&
            key != CharacterAnimationKey.StopWalkR &&
            key != CharacterAnimationKey.StopRunL &&
            key != CharacterAnimationKey.StopRunR &&
            key != CharacterAnimationKey.StopSprintL &&
            key != CharacterAnimationKey.StopSprintR)
        {
            return;
        }

        if (_context.HasMoveInput)
        {
            ChangeSubState(_groundedContext.PreferWalk ? GroundedStateId.Walk : GroundedStateId.Run);
            return;
        }

        ChangeSubState(GroundedStateId.Idle);
    }
}
