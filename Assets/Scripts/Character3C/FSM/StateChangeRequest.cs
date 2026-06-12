

public readonly struct StateChangeRequest
{
    public readonly CharacterStateId From;
    public readonly CharacterStateId To;
    public readonly StatePriority Priority;
    public readonly string Reason;

    public StateChangeRequest(
        CharacterStateId from,
        CharacterStateId to,
        StatePriority priority,
        string reason
    )
    {
        From = from;
        To = to;
        Priority = priority;
        Reason = reason;
    }
}