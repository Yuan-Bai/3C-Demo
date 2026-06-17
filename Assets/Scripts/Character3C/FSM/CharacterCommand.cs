using System.Collections.Generic;

public enum CommandChannel
{
    Locomotion, Action, Traversal,
}

public enum CharacterCommandType
{
    Movestop = CharacterStateId.MoveStop,
    Jump = CharacterStateId.Jump,
    JumpSecond = CharacterStateId.JumpSecond,
    Dash = CharacterStateId.Dash,
    Attack = CharacterStateId.Attack,
    Skill = CharacterStateId.Skill,
    Burst = CharacterStateId.Burst,
    ClimbEnter = CharacterStateId.ClimbEnter,
    ClimbExit = CharacterStateId.ClimbExit,
}

public readonly struct CharacterCommand
{
    public readonly CharacterCommandType Type;
    public readonly CommandChannel Channel;
    public readonly StatePriority Priority;
    public readonly float ExpiresAt;
    public readonly string Reason;
    public CharacterCommand(
        CharacterCommandType type,
        CommandChannel channel,
        StatePriority priority,
        float expiresAt,
        string reason
    )
    {
    Type=type;
    Channel =channel;
    Priority = priority;
    ExpiresAt = expiresAt;
    Reason=reason;
    }
}