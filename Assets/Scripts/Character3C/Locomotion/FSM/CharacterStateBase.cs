


using UnityEngine;

public class LocomotionStateBase : IState<LocomotionStateId>
{
    public LocomotionStateId Id { get; }

    protected Context Context { get; }
    protected StateMachine<LocomotionStateId> StateMachine { get; }

    public LocomotionStateBase(LocomotionStateId id, StateMachine<LocomotionStateId> stateMachine, Context context)
    {
        Id = id;
        StateMachine = stateMachine;
        Context = context;
    }

    public virtual void Enter()
    {
        Debug.Log("进入状态：" + StateMachine.CurrentStateId);
    }

    public virtual void Exit()
    {
        Debug.Log("退出状态：" + StateMachine.CurrentStateId);
    }

    public virtual void Tick(float deltaTime)
    {
    }

    public void ChangeState(LocomotionStateId nextStateId)
    {
        StateMachine.ChangeState(nextStateId);
    }
}