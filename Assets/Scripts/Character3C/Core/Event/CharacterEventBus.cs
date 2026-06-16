using System;
using System.Collections.Generic;
using System.Linq;

public sealed class CharacterEventBus : ICharacterEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();


    public IDisposable Subscribe<T>(Action<T> handler) where T : struct, ICharacterEvent
    {
        Type eventType = typeof(T);
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<Delegate>();
        }

        _handlers[eventType].Add(handler);

        return new Subscription<T>(this, handler);
    }

    public void Publish<T>(T evt) where T : struct, ICharacterEvent
    {
        Type eventType = typeof(T);
        if (!_handlers.TryGetValue(eventType, out var delegates))
            return;

        // 复制一份以避免在遍历时被修改（例如订阅者内取消订阅）
        foreach (var handler in delegates.ToList())
        {
            (handler as Action<T>)?.Invoke(evt);
        }
    }

    private class Subscription<T> : IDisposable where T : struct, ICharacterEvent
    {
        private readonly CharacterEventBus _bus;
        private readonly Action<T> _handler;
        private bool _disposed;

        public Subscription(CharacterEventBus bus, Action<T> handler)
        {
            _bus = bus;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Type eventType = typeof(T);
            if (_bus._handlers.TryGetValue(eventType, out var list))
            {
                list.Remove(_handler);
                if (list.Count == 0)
                    _bus._handlers.Remove(eventType);
            }
        }
    }
}