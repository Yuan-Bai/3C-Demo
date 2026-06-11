

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

    public GroundedStateId GroundedStateId;
    public float GroundedActionElapsedTime;
    public float GroundedDashHeldTime;

    public AirbornePhase AirbornePhase;
    public AirborneActionId AirborneAction;
    public float AirborneActionElapsedTime;

    // 动画驱动位移所需
    public bool UseRootMotion;
    public Vector3 DeltaPosition;
    public Quaternion DeltaRotation;
}
