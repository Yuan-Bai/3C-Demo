using System;
using Animancer;
using UnityEngine;

public interface IAnimancerPort
{
    AnimancerStateHandle Play(CharacterStateId stateId, AnimationId id, float fade = 0.2f);
    AnimancerStateHandle PlayAtNormalized(CharacterStateId stateId, AnimationId id, float normalizedTime, float fade = 0.2f);
    void BindEnd(AnimancerStateHandle handle, Action callback);
    void BindNamedEvent(string eventName, Action callback);
    void SetFloat(string name, float value);
    float GetCurrentNormalizedTime();
}


public readonly struct AnimancerStateHandle
{
    public readonly AnimancerState Raw;
    public AnimancerStateHandle(AnimancerState raw) => Raw = raw;
}
