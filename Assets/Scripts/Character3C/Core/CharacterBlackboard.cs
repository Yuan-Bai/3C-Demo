using UnityEngine;

public enum FootPhase
{
    Unknown,
    Left,
    Right,
}

public sealed class CharacterBlackboard
{
    public bool IsGrounded;
    public Vector3 GroundNormal = Vector3.up;
    public float VerticalSpeed;
    public Vector2 MoveInput;
    public Vector3 DesiredWorldMove;
    public Vector3 Facing = Vector3.forward;
    public int JumpCount;
    public FootPhase LastFootPlant = FootPhase.Unknown;
    public float LocomotionPhase01;
    public bool WantsSprint;
}