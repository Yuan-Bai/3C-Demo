
using UnityEngine;

public sealed class IdleState : CharacterStateBase
{
    public IdleState(CharacterContext ctx) : base(ctx)
    {
    }

    public override CharacterStateId Id => CharacterStateId.Idle;

    public override StatePriority Priority => StatePriority.Locomotion;

    public override void Enter(in StateChangeRequest request)
    {
        base.Enter(request);
        Ctx.Anim.Play(Id, AnimationId.Idle, 0.12f);
    }

    public override void BeforeCharacterUpdate(float deltaTime)
    {
        base.BeforeCharacterUpdate(deltaTime);
        TryToDash();
        if (Bb.HasMoveInput)
        {
            RequestState(CharacterStateId.Move, StatePriority.Locomotion, "move pressed");
            return;
        }
    }


    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = new(0.0f, currentVelocity.y, 0.0f);
    }
}
