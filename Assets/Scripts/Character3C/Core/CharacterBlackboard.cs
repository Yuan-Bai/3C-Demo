using UnityEngine;

public enum FootPhase
{
    Unknown,
    Left,
    Right,
}

public enum MoveMode
{
    Walk,
    Run,
    Sprint,
}

[System.Serializable]
public sealed class CharacterBlackboard
{
    public CharacterInputFrame InputFrame;
    public bool HasMoveInput;
    public Vector3 MoveDirection;
    public Vector3 LookDirection;
    public Vector3 Facing = Vector3.forward;
    public int JumpCount;
    public FootPhase LastFootPlant = FootPhase.Unknown;
    public MoveMode MoveMode = MoveMode.Run;
    public float MoveStopStartTime;
    public float LocomotionPhase01;
    public bool WantsSprint;
    public bool PreferWalk;
}