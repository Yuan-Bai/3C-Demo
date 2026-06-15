public enum CommandChannel
{
    Locomotion, Action, Traversal,
}

public enum CharacterCommandType
{
    Movestop,
    Jump,
    Dash,
    Attack,
    Skill,
    Burst,
    Climb,
}

public readonly struct CharacterCommand
{
    public readonly CharacterCommandType Type;
    public readonly CommandChannel Channel;
    public readonly int Priority;
    public readonly float ExpiresAt;
    public readonly string Reason;
    public CharacterCommand(
        CharacterCommandType type,
        CommandChannel channel,
        int priority,
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