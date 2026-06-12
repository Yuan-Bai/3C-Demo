using System;

public interface ICharacterEvent { }

public interface ICharacterEventBus
{
    IDisposable Subscribe<T>(Action<T> handler) where T : struct, ICharacterEvent;
    void Publish<T>(T evt) where T : struct, ICharacterEvent;
}

// 动画或计时器自然结束时发这个事件。
// 状态只说明“我结束了”，结束后去 idle/move/airborne 由 Coordinator 统一判断。
public readonly struct CharacterStateEndedEvent : ICharacterEvent
{
    public readonly CharacterStateId Source;
    public readonly AnimationId Animation;
    public readonly string Reason;

    public CharacterStateEndedEvent(CharacterStateId source, AnimationId animation, string reason)
    {
        Source = source;
        Animation = animation;
        Reason = reason;
    }
}

// 状态内部不要直接持有状态机；需要切换时发这个请求。
// Coordinator 订阅后会把它转换成 StateChangeRequest 并调用 _stateMachine。
public readonly struct CharacterStateChangeRequestedEvent : ICharacterEvent
{
    public readonly CharacterStateId Source;
    public readonly CharacterStateId Target;
    public readonly StatePriority Priority;
    public readonly string Reason;

    public CharacterStateChangeRequestedEvent(
        CharacterStateId source,
        CharacterStateId target,
        StatePriority priority,
        string reason)
    {
        Source = source;
        Target = target;
        Priority = priority;
        Reason = reason;
    }
}
