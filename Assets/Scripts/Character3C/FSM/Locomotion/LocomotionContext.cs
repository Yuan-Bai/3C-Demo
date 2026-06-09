

using UnityEngine;

public class LocomotionContext
{
    public bool IsStableOnGround;
    public bool HasMoveInput;
    public Vector3 HorizontalSpeed;
    public float VerticalSpeed;
    public Vector3 MoveDirection;
    public Vector3 LookDirection;
    public CharacterInputFrame InputFrame;

    public GroundedGait GroundedGait;
}