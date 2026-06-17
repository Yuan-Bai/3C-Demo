using UnityEngine;

public sealed class FallState : CharacterStateBase
{
    public FallState(CharacterContext ctx) : base(ctx)
    {
    }

    public override CharacterStateId Id => CharacterStateId.Fall;

    public override StatePriority Priority => StatePriority.Airborne;

    public override void Enter(in StateChangeRequest request)
    {
        base.Enter(request);
        Anim.Play(Id, AnimationId.Fall, 0.2f);

    }

    public override void BeforeCharacterUpdate(float deltaTime)
    {
        base.BeforeCharacterUpdate(deltaTime);

        TryToJumpSecond();
    }

    public override void PostGroundingUpdate(float deltaTime)
    {
        base.PostGroundingUpdate(deltaTime);
        if (Ctx.Motor.IsGrounded)
        {
            RequestState(CharacterStateId.Idle, StatePriority.Locomotion, "IsGrounded");
            return;
        }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        currentRotation = Quaternion.LookRotation(Bb.MoveDirection, Ctx.Motor.CharacterUp);
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = new(currentVelocity.x, currentVelocity.y-deltaTime*15f, currentVelocity.z);
    }
}