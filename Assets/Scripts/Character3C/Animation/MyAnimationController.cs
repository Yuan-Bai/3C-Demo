

using System;
using Animancer;
using UnityEngine;

public sealed class MyAnimationController : MonoBehaviour, IAnimancerPort
{
    [SerializeField] private AnimancerComponent _animancer;
    private CharacterDefinitions _defs;

    public void Initialize(CharacterDefinitions defs)
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
        return state != null ? state.NormalizedTime : 0.0f;
    }

    public AnimancerStateHandle Play(AnimationId id, float fade = 0.2f)
    {
        if (_animancer == null || _defs == null)
        {
            return default;
        }

        var transition = _defs.GetTransition(id);
        if (transition != null && transition.IsValid)
        {
            var transitionState = _animancer.Play(transition, _defs.GetFade(id, fade));
            transitionState.Time = 0.0f;
            return new AnimancerStateHandle(transitionState);
        }

        var clip = _defs.GetClip(id);
        if (clip == null)
        {
            return default;
        }

        var state = _animancer.Play(clip, _defs.GetFade(id, fade));
        state.Time = 0.0f;
        return new AnimancerStateHandle(state);
    }

    public AnimancerStateHandle PlayAtNormalized(AnimationId id, float normalizedTime, float fade = 0.2F)
    {
        var handle = Play(id, fade);
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
