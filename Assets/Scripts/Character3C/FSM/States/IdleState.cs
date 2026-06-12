
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
        Ctx.Anim.SetFloat(Ctx.Defs.MoveSpeedParameter, 0.0f);
        Ctx.Anim.Play(AnimationId.Idle, 0.12f);
    }

    public override void Tick(float deltaTime)
    {
        // idle 自己不碰状态机；检测到移动输入后只通过 Bus 请求进入 Move。
        if (HasMoveInput())
        {
            RequestState(CharacterStateId.Move, StatePriority.Locomotion, "move input from idle");
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
