using UnityEngine;

public sealed class MoveState : CharacterStateBase
{
    public MoveState(CharacterContext ctx) : base(ctx)
    {
    }

    public override CharacterStateId Id => CharacterStateId.Move;
    public override StatePriority Priority => StatePriority.Locomotion;

    public override void Enter(in StateChangeRequest request)
    {
        Ctx.Anim.Play(AnimationId.Move, 0.12f);
    }

    public override void Tick(float deltaTime)
    {
        Ctx.Anim.SetFloat(Ctx.Defs.MoveSpeedParameter, Ctx.Bb.DesiredWorldMove.magnitude * Ctx.Defs.MoveSpeed);

        // 松开移动键先进入 MoveStop，而不是直接回 Idle，给停步动画/刹车留一段时间。
        if (!HasMoveInput())
        {
            RequestState(CharacterStateId.MoveStop, StatePriority.Locomotion, "move input released");
        }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (Ctx.Bb.DesiredWorldMove.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(Ctx.Bb.DesiredWorldMove.normalized, Ctx.Motor.CharacterUp);
        float t = 1.0f - Mathf.Exp(-Ctx.Defs.TurnSharpness * deltaTime);
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, t);
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        var desiredVelocity = Ctx.Bb.DesiredWorldMove * Ctx.Defs.MoveSpeed;
        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            desiredVelocity,
            Ctx.Defs.MoveAcceleration * deltaTime);
    }
}
