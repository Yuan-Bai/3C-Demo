

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
    public GroundedActionId GroundedAction;
    public float GroundedActionElapsedTime;
    public float GroundedDashHeldTime;

    public AirbornePhase AirbornePhase;
    public AirborneActionId AirborneAction;
    public float AirborneActionElapsedTime;
}
