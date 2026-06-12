using UnityEngine;

public sealed class DashState : CharacterStateBase
{
    private float _dashSpeed;
    private Vector3 _dashDirection;
    private float _remain;
    private bool _ended;

    public override CharacterStateId Id => CharacterStateId.Dash;
    public override StatePriority Priority => StatePriority.Dash;

    public DashState(CharacterContext ctx) : base(ctx)
    {
    }

    public override void Enter(in StateChangeRequest request)
    {
        // Dash 是主动动作状态：进入时锁定方向和速度，结束后只发 StateEnded。
        // 最终回 Move 还是 Idle 由 Coordinator 根据当前输入和黑板判断。
        _dashDirection = Ctx.Bb.DesiredWorldMove.sqrMagnitude > 0.0001f
            ? Ctx.Bb.DesiredWorldMove.normalized : Ctx.Bb.Facing;

        var def = Ctx.Defs.GetAction(CharacterStateId.Dash);
        _dashSpeed = def.Speed;
        _remain = def.Duration;
        _ended = false;

        Ctx.Motor.ForceUnground(Ctx.Defs.DashUngroundDuration);

        var handle = Ctx.Anim.Play(AnimationId.DashF);
        Ctx.Anim.BindEnd(handle, () =>
        {
            PublishEndOnce("dash anim end");
        });
    }

    public override void Tick(float deltaTime)
    {
        _remain -= deltaTime;
        if (_remain <= 0.0f)
        {
            PublishEndOnce("dash timer end");
        }
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = _dashDirection * _dashSpeed;
    }

    public override bool CanExit(in StateChangeRequest request)
    {
        return _ended || request.Priority > Priority;
    }

    public override void Exit(in StateChangeRequest request)
    {
        _ended = true;
    }

    private void PublishEndOnce(string reason)
    {
        if (_ended)
        {
            return;
        }

        _ended = true;
        Ctx.Bus.Publish(new CharacterStateEndedEvent(Id, AnimationId.DashF, reason));
    }
}
