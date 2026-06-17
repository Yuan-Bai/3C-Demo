

using UnityEngine;

public sealed class JumpSecondState : CharacterStateBase
{
    public JumpSecondState(CharacterContext ctx) : base(ctx)
    {
    }

    public override CharacterStateId Id => CharacterStateId.JumpSecond;

    public override StatePriority Priority => StatePriority.Airborne;


    public override void Enter(in StateChangeRequest request)
    {
        base.Enter(request);
        AnimancerStateHandle handle;
        
        if (Bb.HasMoveInput)
        {
            handle = Anim.Play(Id, AnimationId.JumpSecondF, 0.12f);
        }
        else
        {
            handle = Anim.Play(Id, AnimationId.JumpSecondB, 0.12f);
        }

        Anim.BindEnd(handle, () =>
        { 
            if (Ctx.Motor.IsGrounded)
            {
                if (Bb.HasMoveInput)
                {
                    RequestState(CharacterStateId.Move, StatePriority.Locomotion, "Jump End");
                }
                else
                {
                    RequestState(CharacterStateId.Idle, StatePriority.Locomotion, "Jump End");
                }
            }
            else
            {
                RequestState(CharacterStateId.Fall, StatePriority.Airborne, "Jump End");
            }
        });
    }

    public override void PostGroundingUpdate(float deltaTime)
    {
        base.PostGroundingUpdate(deltaTime);

        if (Ctx.Motor.IsGrounded)
        {
            if (Bb.HasMoveInput)
            {
                RequestState(CharacterStateId.Move, StatePriority.Locomotion, "Jump End");
            }
            else
            {
                RequestState(CharacterStateId.Idle, StatePriority.Locomotion, "Jump End");
            }
        }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (Bb.HasMoveInput)
            currentRotation = Quaternion.LookRotation(Bb.MoveDirection, Ctx.Motor.CharacterUp);
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        Ctx.MotionAccumulator.ConsumeVelocity(Ctx.Motor.IsGrounded, Ctx.Motor.GroundNormal, deltaTime, ref currentVelocity);
    }
}