using UnityEngine;

public sealed class DashState : CharacterStateBase
{
    public override CharacterStateId Id => CharacterStateId.Dash;
    public override StatePriority Priority => StatePriority.Dash;
    // private bool _canBeInterruptByAction;

    public DashState(CharacterContext ctx) : base(ctx)
    {
    }

    public override void Enter(in StateChangeRequest request)
    {
        base.Enter(request);
        AnimancerStateHandle handle;
        if (Bb.HasMoveInput)
        {
            handle = Ctx.Anim.Play(Id, AnimationId.DashF, 0.12f);
        }
        else
        {
            handle = Ctx.Anim.Play(Id, AnimationId.DashB, 0.12f);
        }
        Anim.BindEnd(handle, () =>
        {
            PublishEndOnce("dash anim end");
        });

        var stateData = Ctx.Defs.GetStateData(Id);
        stateData.TryGetAnimationById(AnimationId.DashF, out var animationEntry);

        // _canBeInterruptByAction = false;
        CanTransitionToSelf = false;
        Anim.BindNamedEvent(animationEntry.EventName, CanBeInterruptByAction);
    }

    public override void BeforeCharacterUpdate(float deltaTime)
    {
        base.BeforeCharacterUpdate(deltaTime);

        TryToJump();

        if (!Ctx.Motor.IsGrounded)
        {
            RequestState(CharacterStateId.Fall, StatePriority.Airborne, "Not at Grounded");
            return;
        }
        if (Anim.GetCurrentNormalizedTime() >= 0.55)
        {
            if (Bb.HasMoveInput)
            {
                RequestState(CharacterStateId.Move, StatePriority.Locomotion, "move pressed");
                return;
            }
        }

        if (Ctx.Bb.InputFrame.DashPressed)
        {
            Commands.Push(new CharacterCommand(
                CharacterCommandType.Dash,
                CommandChannel.Action,
                Priority,
                Time.time + 0.5f,
                ""
            ), Time.time);
        }
    }


    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        Ctx.MotionAccumulator.ConsumeVelocity(Ctx.Motor.IsGrounded, Ctx.Motor.GroundNormal, deltaTime, ref currentVelocity);
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (Bb.HasMoveInput)
            currentRotation = Quaternion.LookRotation(Bb.MoveDirection, Ctx.Motor.CharacterUp);
    }

    private void PublishEndOnce(string reason)
    {
        RequestState(CharacterStateId.Idle, StatePriority.Locomotion, "Dash End");
    }

    private void CanBeInterruptByAction()
    {
        // _canBeInterruptByAction = true;
        CanTransitionToSelf = true;
    }
}
