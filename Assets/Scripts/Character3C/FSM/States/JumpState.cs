

using UnityEngine;

public sealed class JumpState : CharacterStateBase
{
    public JumpState(CharacterContext ctx) : base(ctx)
    {
    }

    public override CharacterStateId Id => CharacterStateId.Jump;

    public override StatePriority Priority => StatePriority.Airborne;

    private Vector3 _beforeJumpVelocity = Vector3.zero;

    public override void Enter(in StateChangeRequest request)
    {
        base.Enter(request);
        var handle = Ctx.Anim.Play(Id, AnimationId.JumpL, 0.12f);
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

        _beforeJumpVelocity = Ctx.Motor.Velocity;

        // 强制解除地面状态一小段时间，防止下一帧又被吸附回地面
        Ctx.Motor.ForceUnground(0.1f);
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
        // Debug.Log(currentVelocity);
        currentVelocity += _beforeJumpVelocity;
    }
}