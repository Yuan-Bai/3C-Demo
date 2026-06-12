

public sealed class CharacterContext
{
    public readonly IKccMotorPort Motor;
    public readonly IAnimancerPort Anim;
    public readonly ICharacterEventBus Bus;
    public readonly CharacterCommandBuffer Commands;
    public readonly CharacterBlackboard Bb;
    public readonly CharacterDefinitions Defs;

    public float FrameDeltaTime;
    public float TickDeltaTime;

    public CharacterContext(
        IKccMotorPort motor,
        IAnimancerPort anim,
        ICharacterEventBus bus,
        CharacterCommandBuffer commands,
        CharacterBlackboard bb,
        CharacterDefinitions defs
    )
    {
        Motor = motor;
        Anim = anim;
        Bus = bus;
        Commands = commands;
        Bb = bb;
        Defs = defs;
    }
}