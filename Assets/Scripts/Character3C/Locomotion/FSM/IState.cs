using System;

public interface IState<TStateId> where TStateId : Enum
{
    public TStateId Id { get; }
    public void Enter();
    public void Exit();
    public void Tick(float deltaTime);
}