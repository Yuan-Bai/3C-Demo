

public sealed class CharacterContext
{
    public readonly IKccMotorPort Motor;
    public readonly IAnimancerPort Anim;
    public readonly ICharacterEventBus Bus;
    public readonly CharacterCommandBuffer Commands;
    public readonly CharacterBlackboard Bb;
    public readonly AnimationStateDatabase Defs;
    public readonly RootMotionAccumulator MotionAccumulator;

    public float FrameDeltaTime;
    public float TickDeltaTime;

    public CharacterContext(
        IKccMotorPort motor,
        IAnimancerPort anim,
        ICharacterEventBus bus,
        CharacterCommandBuffer commands,
        CharacterBlackboard bb,
        AnimationStateDatabase defs,
        RootMotionAccumulator motionAccumulator
    )
    {
        Motor = motor;
        Anim = anim;
        Bus = bus;
        Commands = commands;
        Bb = bb;
        Defs = defs;
        MotionAccumulator = motionAccumulator;
    }
}