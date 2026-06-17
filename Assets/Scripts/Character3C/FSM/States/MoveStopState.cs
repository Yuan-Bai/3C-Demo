using UnityEngine;

public sealed class MoveStopState : CharacterStateBase
{
    public MoveStopState(CharacterContext ctx) : base(ctx)
    {
    }

    public override CharacterStateId Id => CharacterStateId.MoveStop;
    public override StatePriority Priority => StatePriority.Locomotion;

    public override void Enter(in StateChangeRequest request)
    {
        base.Enter(request);
        AnimationId animationId = AnimationId.StopRunL;
        if (Bb.LastFootPlant == FootPhase.Left)
        {
            if (Bb.MoveMode == MoveMode.Walk)
            {
                animationId = AnimationId.StopWalkL;
            }
            else if (Bb.MoveMode == MoveMode.Run)
            {
                animationId = AnimationId.StopRunL;
            }
            else
            {
                animationId = AnimationId.StopSprintL;
            }
        }
        else if (Bb.LastFootPlant == FootPhase.Right)
        {
            if (Bb.MoveMode == MoveMode.Walk)
            {
                animationId = AnimationId.StopWalkR;
            }
            else if (Bb.MoveMode == MoveMode.Run)
            {
                animationId = AnimationId.StopRunR;
            }
            else
            {
                animationId = AnimationId.StopSprintR;
            }
        }

        var handle = Ctx.Anim.PlayAtNormalized(Id, animationId, Bb.MoveStopStartTime);

        Ctx.Anim.BindEnd(handle, () =>
        {
            RequestState(CharacterStateId.Idle, StatePriority.Locomotion, "Movestop End");
        });
    }

    public override void BeforeCharacterUpdate(float deltaTime)
    {
        base.BeforeCharacterUpdate(deltaTime);

        TryToDash();
        TryToJump();

        if (!Ctx.Motor.IsGrounded)
        {
            RequestState(CharacterStateId.Fall, StatePriority.Airborne, "Not at Grounded");
            return;
        }

        if (Bb.HasMoveInput)
        {
            RequestState(CharacterStateId.Move, StatePriority.Locomotion, "move pressed");
            return;
        }
    }


    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        Ctx.MotionAccumulator.ConsumeVelocity(Ctx.Motor.IsGrounded, Ctx.Motor.GroundNormal, deltaTime, ref currentVelocity);
    }
}
