
public enum CharacterStateId
{
    Idle,
    Move,
    SprintImpulse,
    MoveStop,
    Jump,
    JumpSecond,
    Rise,
    Fall,
    ClimbEnter,
    ClimbLoop,
    ClimbExit,
    Dash,
    Attack,
    Skill,
    Burst,
}

public enum StatePriority
{
    Locomotion = 0,
    Airborne = 10,
    Attack = 20,
    Dash = 30,
    Skill = 40,
    Burst = 50,
    Traversal = 60,
}