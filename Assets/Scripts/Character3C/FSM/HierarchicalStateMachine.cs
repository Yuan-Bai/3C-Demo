
using System.Collections.Generic;
using UnityEngine;

public class HierarchicalStateMachine
{
    private Dictionary<CharacterStateId, ICharacterState> _states = new();
    private ICharacterState _currentState;

    public ICharacterState CurrentState => _currentState;
    public CharacterStateId CurrentStateId {get; private set;}

    public void AddState(ICharacterState state)
    {
        _states[state.Id] = state;
    }

    // public bool TryChange(in StateChangeRequest request)
    // {
    //     if (_currentState != null && EqualityComparer<CharacterStateId>.Default.Equals(_currentState.Id, request.To))
    //     {
    //         return false;
    //     }

    //     if (!_states.TryGetValue(request.To, out var state))
    //     {
    //         return false;
    //     }
        
    //     if (_currentState != null && !_currentState.CanExit(request))
    //     {
    //         return false;
    //     }

    //     if (!state.CanEnter(request))
    //     {
    //         return false;
    //     }

    //     _currentState?.Exit(request);
    //     _currentState = state;
    //     CurrentStateId = request.To;
    //     _currentState.Enter(request);
    //     return true;
    // }

    public bool ForceChange(in StateChangeRequest request)
    {
        if (_currentState != null && EqualityComparer<CharacterStateId>.Default.Equals(_currentState.Id, request.To))
        {
            Debug.Log("change");
            if (!_currentState.CanTransitionToSelf)
                return false;
        }

        if (!_states.TryGetValue(request.To, out var state))
        {
            return false;
        }

        _currentState?.Exit(request);
        _currentState = state;
        CurrentStateId = request.To;
        _currentState.Enter(request);
        return true;
    }
}
