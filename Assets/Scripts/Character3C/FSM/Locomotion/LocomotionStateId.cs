public enum LocomotionStateId
{
    Grounded,
    Airborne,
    Climbing,
}

public enum GroundedGait
{
    Idle,
    Walk,
    Run,
    Sprint,
}

public enum AirbornePhase
{
    Rising,
    Falling,
}

public enum GroundedActionId
{
    None,
    Dash,
    SprintImpulse,
    MoveStop,
    JumpStart,
}

public enum AirborneActionId
{
    None,
    Jump,
    JumpSecond,
}

public enum ClimbActionId
{
    None,
    Start,
    UpLeft,
    UpRight,
    DownLeft,
    DownRight,
    SideLeft,
    SideRight,
    Dash,
    ToTop,
}