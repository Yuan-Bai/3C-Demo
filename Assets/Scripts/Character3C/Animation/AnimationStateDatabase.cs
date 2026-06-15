

using System.Collections.Generic;
using Animancer;
using UnityEngine;

[CreateAssetMenu(menuName = "Character3C/Animation State Database", fileName = "AnimationStateDatabase")]
public class AnimationStateDatabase : ScriptableObject
{
    public List<StateData> States = new List<StateData>();

    private Dictionary<CharacterStateId, StateData> _stateMap;
    private float _moveInputDeadZone = 0.01f;

    public StateData GetStateData(CharacterStateId StateId)
    {
        if (_stateMap == null)
        {
            _stateMap = new Dictionary<CharacterStateId, StateData>();
            foreach (var state in States)
                if (!_stateMap.ContainsKey(state.StateId))
                    _stateMap.Add(state.StateId, state);
        }
        _stateMap.TryGetValue(StateId, out var result);
        return result;
    }

    public bool HasMoveInput(Vector3 input)
    {
        return input.sqrMagnitude >= _moveInputDeadZone * _moveInputDeadZone;
    }
}

[System.Serializable]
public class AnimationEntry
{
    [Tooltip("Animancer资产（Clip/Mixer/Controller）")]
    public TransitionAsset Transition;
    public StringAsset Parameter;

    [Header("动画参数")]
    public float startTime = 0.0f;
    public float Speed = 1.0f;
    public float FadeDuration = 0.25f;
    public bool UseRootMotionXZ = false;
    public bool UseRootMotionY = false;

    [Tooltip("混合器参数映射曲线")]
    public AnimationCurve MixerParameterCurve = AnimationCurve.Linear(0,0,1,1);
}

[System.Serializable]
public struct AnimationEntryDictionary
{
    public AnimationId animationId;
    public AnimationEntry animationEntry;
}

[System.Serializable]
public class StateData
{
    [Header("状态标识")]
    public CharacterStateId StateId;

    [Header("状态逻辑参数")]
    public int Priority = 0;
    public bool CanSelfInterrupt = false;
    public bool RequireGround = false;

    [Header("动画配置（多选一，互斥播放）")]
    public List<AnimationEntryDictionary> AnimationList = new();

    public bool TryGetAnimationById(AnimationId animationId, out AnimationEntry animationEntry)
    {
        animationEntry = AnimationList.Find(e => e.animationId == animationId).animationEntry;
        if (animationEntry == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
