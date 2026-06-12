using UnityEngine;

public abstract class CharacterStateBase : ICharacterState
{
    public abstract CharacterStateId Id {get;}

    public abstract StatePriority Priority {get;}

    protected readonly CharacterContext Ctx;

    protected CharacterStateBase(CharacterContext ctx)
    {
        Ctx = ctx;
    }

    protected void RequestState(CharacterStateId target, StatePriority priority, string reason)
    {
        Ctx.Bus.Publish(new CharacterStateChangeRequestedEvent(Id, target, priority, reason));
    }

    protected bool HasMoveInput()
    {
        return Ctx.Defs.HasMoveInput(Ctx.Bb.MoveInput);
    }

    public virtual bool CanEnter(in StateChangeRequest request)
    {
        return true;
    }

    public virtual bool CanExit(in StateChangeRequest request)
    {
        return request.Priority >= Priority;
    }

    public virtual void Enter(in StateChangeRequest request)
    {
    }

    public virtual void Exit(in StateChangeRequest request)
    {
    }

    public virtual void Tick(float deltaTime)
    {
    }

    public virtual void AfterCharacterUpdate(float deltaTime)
    {
    }

    public virtual void BeforeCharacterUpdate(float deltaTime)
    {
    }

    public virtual void PostGroundingUpdate(float deltaTime)
    {
    }

    public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
    }

    public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
    }
}
