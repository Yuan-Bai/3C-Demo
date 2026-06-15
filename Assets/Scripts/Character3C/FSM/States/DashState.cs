using UnityEngine;

public sealed class DashState : CharacterStateBase
{
    private bool _ended;

    public override CharacterStateId Id => CharacterStateId.Dash;
    public override StatePriority Priority => StatePriority.Dash;

    public DashState(CharacterContext ctx) : base(ctx)
    {
    }

    public override void Enter(in StateChangeRequest request)
    {
        var handle = Ctx.Anim.Play(Id, AnimationId.DashF);
        Ctx.Anim.BindEnd(handle, () =>
        {
            PublishEndOnce("dash anim end");
        });
        _ended = false;
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        Ctx.MotionAccumulator.ConsumeVelocity(Ctx.Motor.IsGrounded, Ctx.Motor.GroundNormal, deltaTime, ref currentVelocity);
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        currentRotation = Quaternion.LookRotation(Bb.MoveDirection, Ctx.Motor.CharacterUp);
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
        RequestState(CharacterStateId.Idle, StatePriority.Locomotion, "Dash End");
    }
}
