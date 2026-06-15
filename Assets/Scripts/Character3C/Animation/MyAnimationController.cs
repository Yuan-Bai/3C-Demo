

using System;
using Animancer;
using UnityEngine;

public sealed class MyAnimationController : MonoBehaviour, IAnimancerPort
{
    [SerializeField] private AnimancerComponent _animancer;
    private AnimationStateDatabase _defs;
    
    public void Initialize(AnimationStateDatabase defs)
    {
        _animancer ??= GetComponent<AnimancerComponent>();
        _defs = defs;
    }

    public void BindEnd(AnimancerStateHandle handle, Action callback)
    {
        if (handle.Raw == null)
        {
            return;
        }

        handle.Raw.Events(this).OnEnd = callback;
    }

    public void BindNamedEvent(string eventName, Action callback)
    {
        var state = _animancer != null ? _animancer.Graph.Layers[0].CurrentState : null;
        if (state == null)
        {
            return;
        }

        state.Events(this).SetCallback(eventName, callback);
    }

    public float GetCurrentNormalizedTime()
    {
        var state = _animancer != null ? _animancer.Graph.Layers[0].CurrentState : null;
        if (state == null)
            return 0.0f;
        
        // 使用 Mathf.Repeat 或 % 1.0f 均可，Mathf.Repeat 对负数也更安全
        return Mathf.Repeat(state.NormalizedTime, 1.0f);
        // 或者：return state.NormalizedTime % 1.0f;
    }

    public AnimancerStateHandle Play(CharacterStateId stateId, AnimationId id, float fade = 0.2f)
    {
        if (_animancer == null || _defs == null)
        {
            return default;
        }

        StateData stateData = _defs.GetStateData(stateId);

        if (!stateData.TryGetAnimationById(id, out AnimationEntry entry))
        {
            return default;
        }
        float fadeDuration = entry.FadeDuration > 0.0f ? entry.FadeDuration : fade;
        AnimancerState transitionState = _animancer.Play(entry.Transition, fadeDuration);
        transitionState.Time = entry.startTime;
        transitionState.Speed = Mathf.Approximately(entry.Speed, 0.0f) ? 1.0f : entry.Speed;
        return new AnimancerStateHandle(transitionState);
    }

    public AnimancerStateHandle PlayAtNormalized(CharacterStateId stateId, AnimationId id, float normalizedTime, float fade = 0.2F)
    {
        var handle = Play(stateId, id, fade);
        if (handle.Raw != null)
        {
            handle.Raw.NormalizedTime = normalizedTime;
        }

        return handle;
    }

    public void SetFloat(string name, float value)
    {
        if (_animancer == null || string.IsNullOrEmpty(name))
        {
            return;
        }

        _animancer.Graph.Parameters.SetValue<float>(name, value);
    }
}
