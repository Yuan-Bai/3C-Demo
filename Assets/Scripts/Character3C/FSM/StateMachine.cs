

using System;
using System.Collections.Generic;

public class StateMachine<TStateId> where TStateId : Enum
{
    private readonly Dictionary<TStateId, IState<TStateId>> _states = new();
    private IState<TStateId> _currentState;
    public IState<TStateId> CurrentState => _currentState;
    public TStateId CurrentStateId {get; private set;}

    public void AddState(IState<TStateId> state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (_states.ContainsKey(state.Id))
        {
            throw new InvalidOperationException($"State already registered: {state.Id}.");
        }

        _states[state.Id] = state;
    }

    public bool ChangeState(TStateId newState)
    {
        // 如果当前状态不为空，新状态和当前状态一致返回false
        if (_currentState != null && EqualityComparer<TStateId>.Default.Equals(_currentState.Id, newState))
        {
            return false;
        }
        
        if (!_states.TryGetValue(newState, out var nextState))
        {
            throw new InvalidOperationException($"State not registered: {newState}.");
        }

        _currentState?.Exit();
        _currentState = nextState;
        // 有些状态的enter会做判断，触发changestate，所以CurrentStateId的修改需要在enter前
        CurrentStateId = _currentState.Id;
        _currentState.Enter();
        return true;
    }

    public void Tick(float deltaTime)
    {
        _currentState?.Tick(deltaTime);
    }
}
