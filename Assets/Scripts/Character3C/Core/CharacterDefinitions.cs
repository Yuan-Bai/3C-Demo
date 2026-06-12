using Animancer;
using UnityEngine;

[CreateAssetMenu(menuName = "Character3C/Character Definitions")]
public sealed class CharacterDefinitions : ScriptableObject
{
    // Definitions 是静态数据表：动画 Transition/Clip、动作速度/时长、移动参数都从这里读。
    // Move 这类连续动作建议配置 Animancer Linear/2D Mixer 的 Transition Asset。
    // Idle/Dash/MoveStop 也可以先直接配置 AnimationClip，端口会自动 fallback。
    // 没配数据时脚本会使用默认值，方便先验证状态流，再慢慢补动画资源。
    [SerializeField] private AnimationEntry[] _animations = new AnimationEntry[0];
    [SerializeField] private ActionDefinition[] _actions = new ActionDefinition[0];

    [Header("Locomotion")]
    [SerializeField] private float _moveInputDeadZone = 0.12f;
    [SerializeField] private StringAsset _moveSpeedParameter;
    [SerializeField] private float _moveSpeed = 4.5f;
    [SerializeField] private float _moveAcceleration = 24.0f;
    [SerializeField] private float _groundBraking = 30.0f;
    [SerializeField] private float _turnSharpness = 18.0f;
    [SerializeField] private float _moveStopDuration = 0.25f;

    [Header("Dash")]
    [SerializeField] private float _dashSpeed = 7.0f;
    [SerializeField] private float _dashDuration = 0.35f;
    [SerializeField] private float _dashUngroundDuration = 0.1f;
    [SerializeField] private float _dashCommandBufferTime = 0.08f;

    public float MoveInputDeadZone => _moveInputDeadZone;
    public string MoveSpeedParameter => _moveSpeedParameter;
    public float MoveSpeed => _moveSpeed;
    public float MoveAcceleration => _moveAcceleration;
    public float GroundBraking => _groundBraking;
    public float TurnSharpness => _turnSharpness;
    public float MoveStopDuration => _moveStopDuration;
    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;
    public float DashUngroundDuration => _dashUngroundDuration;
    public float DashCommandBufferTime => _dashCommandBufferTime;

    public bool HasMoveInput(Vector2 input)
    {
        return input.sqrMagnitude >= _moveInputDeadZone * _moveInputDeadZone;
    }

    public bool TryGetAnimation(AnimationId id, out AnimationEntry entry)
    {
        if (_animations != null)
        {
            for (int i = 0; i < _animations.Length; i++)
            {
                if (_animations[i].AnimationId == id)
                {
                    entry = _animations[i];
                    return true;
                }
            }
        }

        entry = default;
        return false;
    }

    public TransitionAssetBase GetTransition(AnimationId id)
    {
        return TryGetAnimation(id, out var entry) ? entry.Transition : null;
    }

    public AnimationClip GetClip(AnimationId id)
    {
        return TryGetAnimation(id, out var entry) ? entry.AnimationClip : null;
    }

    public float GetFade(AnimationId id, float fallback = 0.2f)
    {
        if (TryGetAnimation(id, out var entry))
        {
            return entry.FadeDuration > 0.0f ? entry.FadeDuration : fallback;
        }

        return fallback;
    }

    public ActionDefinition GetAction(CharacterStateId state) 
    {
        if (_actions != null)
        {
            for (int i = 0; i < _actions.Length; i++)
            {
                if (_actions[i].State == state)
                {
                    return _actions[i];
                }
            }
        }

        return ActionDefinition.CreateDefault(state, this);
    }
}

[System.Serializable]
public struct AnimationEntry
{
    public AnimationId AnimationId;

    [Tooltip("Animancer Transition Asset. Use this for mixers such as LinearMixer/DirectionalMixer.")]
    public TransitionAssetBase Transition;

    [Tooltip("Fallback for simple one-shot or looping clips when no Transition Asset is assigned.")]
    public AnimationClip AnimationClip;

    public float FadeDuration;
}

[System.Serializable]
public struct ActionDefinition
{
    public CharacterStateId State;
    public float Speed;
    public float Duration;
    public StatePriority Priority;
    public bool UseRootmotionXZ;
    public bool UseRootmotionY;

    public static ActionDefinition CreateDefault(CharacterStateId state, CharacterDefinitions defs)
    {
        return new ActionDefinition
        {
            State = state,
            Speed = state == CharacterStateId.Dash ? defs.DashSpeed : defs.MoveSpeed,
            Duration = state == CharacterStateId.Dash ? defs.DashDuration : defs.MoveStopDuration,
            Priority = ResolveDefaultPriority(state),
            UseRootmotionXZ = false,
            UseRootmotionY = false,
        };
    }

    private static StatePriority ResolveDefaultPriority(CharacterStateId state)
    {
        return state switch
        {
            CharacterStateId.Dash => StatePriority.Dash,
            CharacterStateId.Attack => StatePriority.Attack,
            CharacterStateId.Skill => StatePriority.Skill,
            CharacterStateId.Burst => StatePriority.Burst,
            _ => StatePriority.Locomotion,
        };
    }
}
