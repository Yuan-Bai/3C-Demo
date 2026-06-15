using Animancer;
using UnityEngine;

public sealed class MoveState : CharacterStateBase
{
    private float _speed = 2.0f;
    private StateData _stateData;
    private StringAsset _parameter;

    public MoveState(CharacterContext ctx) : base(ctx)
    {
    }

    public override CharacterStateId Id => CharacterStateId.Move;
    public override StatePriority Priority => StatePriority.Locomotion;

    public override void Enter(in StateChangeRequest request)
    {
        Ctx.Anim.Play(Id, AnimationId.Move, 0.12f);
        _stateData = Ctx.Defs.GetStateData(Id);
        _stateData.TryGetAnimationById(AnimationId.Move, out var animationEntry);
        _parameter = animationEntry.Parameter;
    }

    public override void BeforeCharacterUpdate(float deltaTime)
    {
        base.BeforeCharacterUpdate(deltaTime);
        TryToMoveStop();
        TryToDash();

        if (Ctx.Bb.WantsSprint)
        {
            Bb.MoveMode = MoveMode.Sprint;
            _speed = 4.0f;
        }
        else
        {
            if (Ctx.Bb.PreferWalk)
            {
                Bb.MoveMode = MoveMode.Walk;
                _speed = 0.8f;
            }
            else
            {
                Bb.MoveMode = MoveMode.Run;
                _speed = 2.0f;
            }
        }
    }

    public override void AfterCharacterUpdate(float deltaTime)
    {
        base.AfterCharacterUpdate(deltaTime);
        Ctx.Anim.SetFloat(_parameter, _speed);
    }



    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (Bb.LookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(Bb.MoveDirection, Ctx.Motor.CharacterUp);
        float t = 1.0f - Mathf.Exp(-18f * deltaTime);
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, t);
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        var desiredVelocity = Ctx.Bb.MoveDirection * _speed;
        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            desiredVelocity,
            20f * deltaTime);
    }
}
