


using UnityEngine;

public class LocomotionStateBase : IState<LocomotionStateId>
{
    public LocomotionStateId Id { get; }

    protected LocomotionContext Context { get; }
    protected StateMachine<LocomotionStateId> StateMachine { get; }
    protected CharacterInputFrame InputFrame => Context.InputFrame;

    public LocomotionStateBase(LocomotionStateId id, StateMachine<LocomotionStateId> stateMachine, LocomotionContext context, ICharacterAnimationDriver animation)
    {
        Id = id;
        StateMachine = stateMachine;
        Context = context;
    }

    public virtual void Enter()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void Tick(float deltaTime)
    {
    }

    public void ChangeState(LocomotionStateId nextStateId)
    {
        StateMachine.ChangeState(nextStateId);
    }
}
