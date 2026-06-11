
public class ClimbingState : LocomotionStateBase
{
    public ClimbingState(LocomotionStateId id, StateMachine<LocomotionStateId> stateMachine, LocomotionContext context, ICharacterAnimationDriver animation) : base(id, stateMachine, context, animation)
    {
    }
}