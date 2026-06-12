using UnityEngine;

public sealed class MoveStopState : CharacterStateBase
{
    private float _remain;

    public MoveStopState(CharacterContext ctx) : base(ctx)
    {
    }

    public override CharacterStateId Id => CharacterStateId.MoveStop;
    public override StatePriority Priority => StatePriority.Locomotion;

    public override void Enter(in StateChangeRequest request)
    {
        _remain = Ctx.Defs.MoveStopDuration;
        Ctx.Anim.SetFloat(Ctx.Defs.MoveSpeedParameter, 0.0f);

        var stopAnimation = Ctx.Bb.LastFootPlant == FootPhase.Right
            ? AnimationId.StopRunR
            : AnimationId.StopRunL;

        var handle = Ctx.Anim.Play(stopAnimation, 0.08f);
        Ctx.Anim.BindEnd(handle, () =>
        {
            Ctx.Bus.Publish(new CharacterStateEndedEvent(Id, stopAnimation, "movestop anim end"));
        });
    }

    public override void Tick(float deltaTime)
    {
        // 停步过程中重新推杆，立刻恢复 Move；否则等动画或计时器结束后回 Idle。
        if (HasMoveInput())
        {
            RequestState(CharacterStateId.Move, StatePriority.Locomotion, "move input during movestop");
            return;
        }

        _remain -= deltaTime;
        if (_remain <= 0.0f)
        {
            RequestState(CharacterStateId.Idle, StatePriority.Locomotion, "movestop timer end");
        }
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            Vector3.zero,
            Ctx.Defs.GroundBraking * deltaTime);
    }
}
