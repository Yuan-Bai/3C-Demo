
[System.Serializable]
public readonly struct AnimationCommand
{
    public CharacterAnimationKey Key { get; }
    public bool NotifyEnd { get; }
    public bool UseRootMotion { get; }
    public float FadeDuration { get; }
    public bool Restart { get; }

    public AnimationCommand(
        CharacterAnimationKey key,
        bool notifyEnd,
        bool useRootMotion,
        float fadeDuration,
        bool restart)
    {
        Key = key;
        NotifyEnd = notifyEnd;
        UseRootMotion = useRootMotion;
        FadeDuration = fadeDuration;
        Restart = restart;
    }
}

public interface ICharacterAnimationDriver
{
    void Play(AnimationCommand command);
}