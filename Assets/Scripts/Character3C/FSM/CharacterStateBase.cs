using UnityEngine;

public abstract class CharacterStateBase : ICharacterState
{
    public abstract CharacterStateId Id {get;}

    public abstract StatePriority Priority {get;}

    public bool CanTransitionToSelf {get; protected set;} = false;

    protected readonly CharacterContext Ctx;
    protected readonly CharacterBlackboard Bb;
    protected readonly CharacterCommandBuffer Commands;
    protected readonly IAnimancerPort Anim;

    protected CharacterStateBase(CharacterContext ctx)
    {
        Ctx = ctx;
        Bb = ctx.Bb;
        Commands = ctx.Commands;
        Anim = ctx.Anim;
    }

    protected void RequestState(CharacterStateId target, StatePriority priority, string reason)
    {
        Ctx.Bus.Publish(new CharacterStateChangeRequestedEvent(Id, target, priority, reason));
    }

    public virtual bool CanEnter(in StateChangeRequest request)
    {
        Debug.Log("进入："+Id);
        return true;
    }

    public virtual bool CanExit(in StateChangeRequest request)
    {
        Debug.Log("退出："+Id);
        return request.Priority >= Priority;
    }

    public virtual void Enter(in StateChangeRequest request)
    {
        Debug.Log("进入："+Id);
        Ctx.MotionAccumulator.ClearMove();
    }

    public virtual void Exit(in StateChangeRequest request)
    {
        Debug.Log("退出："+Id);
        Ctx.MotionAccumulator.ClearMove();
    }

    public virtual void Tick(float deltaTime)
    {
    }

    public virtual void AfterCharacterUpdate(float deltaTime)
    {
    }

    public virtual void BeforeCharacterUpdate(float deltaTime)
    {
    }

    public virtual void PostGroundingUpdate(float deltaTime)
    {
    }

    public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
    }

    public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
    }

    #region 便捷判断切换状态函数
    protected bool TryToMoveStop()
    {
        if (!Ctx.Bb.HasMoveInput)
        {
            if (Bb.MoveMode == MoveMode.Walk)
            {
                if (Anim.GetCurrentNormalizedTime()<=0.25f)
                {
                    Bb.LastFootPlant = FootPhase.Left;
                    // 乘以4=除以0.25
                    Bb.MoveStopStartTime = 0.08f*Anim.GetCurrentNormalizedTime()*4;
                }
                else if (Anim.GetCurrentNormalizedTime()<=0.5f)
                {
                    return false;
                }
                else if (Anim.GetCurrentNormalizedTime()<=0.75f)
                {
                    Bb.LastFootPlant = FootPhase.Right;
                    Bb.MoveStopStartTime = 0.08f*(Anim.GetCurrentNormalizedTime()-0.5f)*4;
                }
                else
                {
                    return false;
                }
            }
            else if (Bb.MoveMode == MoveMode.Run)
            {
                // Debug.Log("Bb.LastFootPlant: "+Anim.GetCurrentNormalizedTime());
                if (Anim.GetCurrentNormalizedTime()<=0.1f)
                {
                    return false;
                }
                else if (Anim.GetCurrentNormalizedTime()<=0.25f)
                {
                    Bb.LastFootPlant = FootPhase.Left;
                    Bb.MoveStopStartTime = 0.05f*(Anim.GetCurrentNormalizedTime()-0.1f)*6.67f;
                }
                else if (Anim.GetCurrentNormalizedTime()<=0.6f)
                {
                    return false;
                }
                else if (Anim.GetCurrentNormalizedTime()<=0.75f)
                {
                    Bb.LastFootPlant = FootPhase.Right;
                    Bb.MoveStopStartTime = 0.05f*(Anim.GetCurrentNormalizedTime()-0.6f)*6.67f;
                }
                else
                {
                    return false;
                }
            }
            else if (Bb.MoveMode == MoveMode.Sprint)
            {
                if (Anim.GetCurrentNormalizedTime()<=0.25f)
                {
                    Bb.LastFootPlant = FootPhase.Left;
                    // 乘以4=除以0.25
                    Bb.MoveStopStartTime = 0.02f*Anim.GetCurrentNormalizedTime()*4;
                }
                else if (Anim.GetCurrentNormalizedTime()<=0.5f)
                {
                    return false;
                }
                else if (Anim.GetCurrentNormalizedTime()<=0.75f)
                {
                    Bb.LastFootPlant = FootPhase.Right;
                    Bb.MoveStopStartTime = 0.02f*(Anim.GetCurrentNormalizedTime()-0.5f)*4;
                }
                else
                {
                    return false;
                }
            }
            Commands.Push(new CharacterCommand(
                CharacterCommandType.Movestop,
                CommandChannel.Locomotion,
                Priority,
                Time.time + 0.2f,
                ""
            ), Time.time);
            return true;
        }
        else
        {
            return false;
        }
    }

    protected bool TryToDash()
    {
        if (Ctx.Bb.InputFrame.DashPressed)
        {
            Commands.Push(new CharacterCommand(
                CharacterCommandType.Dash,
                CommandChannel.Action,
                Priority,
                Time.time + 0.2f,
                ""
            ), Time.time);
            return true;
        }
        else
        {
            return false;
        }
    }

    protected bool TryToJump()
    {
        if (Bb.InputFrame.JumpPressed)
        {
            Commands.Push(new CharacterCommand(
                CharacterCommandType.Jump,
                CommandChannel.Action,
                Priority,
                Time.time + 0.2f,
                ""
            ), Time.time);
            return true;
        }
        else
        {
            return false;
        }
    }

    protected bool TryToJumpSecond()
    {
        if (Bb.InputFrame.DashPressed)
        {
            Commands.Push(new CharacterCommand(
                CharacterCommandType.JumpSecond,
                CommandChannel.Action,
                Priority,
                Time.time + 0.2f,
                ""
            ), Time.time);
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion
}
