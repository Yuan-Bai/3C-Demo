using Animancer;
using UnityEngine;


[RequireComponent(typeof(AnimancerComponent))]
public class CharacterAnimancerController : MonoBehaviour, ICharacterAnimationDriver
{
    [SerializeField] private AnimancerComponent _animancer;
    // [SerializeField] private bool _disableRootMotion = true;
    [SerializeField] private bool _lockAnimatedRootToInitialLocalPose;
    [SerializeField] private Transform _animatedRoot;

    [Header("Animation Asset")]
    [SerializeField] private LinearMixerTransition _move;
    [SerializeField] private ClipTransition _dashF;
    [SerializeField] private ClipTransition _dashB;
    [SerializeField] private ClipTransition _sprintImpulse;
    [SerializeField] private ClipTransition _stopWalkL;
    [SerializeField] private ClipTransition _stopWalkR;
    [SerializeField] private ClipTransition _stopRunL;
    [SerializeField] private ClipTransition _stopRunR;
    [SerializeField] private ClipTransition _stopSprintL;
    [SerializeField] private ClipTransition _stopSprintR;
    [SerializeField] private ClipTransition _jumpWalkL;
    [SerializeField] private ClipTransition _jumpWalkR;
    [SerializeField] private ClipTransition _jumpRunL;
    [SerializeField] private ClipTransition _jumpRunR;
    [SerializeField] private ClipTransition _jumpSecondF;
    [SerializeField] private ClipTransition _jumpSecondB;
    [SerializeField] private ClipTransition _fall;

    [Header("Animation Parameter")]
    [SerializeField] private StringAsset _speedParameter;

    private CharacterAnimationKey _currentKey;
    private AnimancerState _currentState;
    private Vector3 _initialAnimatedRootLocalPosition;
    private Quaternion _initialAnimatedRootLocalRotation;
    private LocomotionContext _locomotionContext;
    private SmoothedFloatParameter _speedSmoothedParameter;

    public event System.Action<CharacterAnimationKey> AnimationEnded;

    private void Awake()
    {
        _animancer ??= GetComponent<AnimancerComponent>();

        if (_animatedRoot == null && _animancer != null)
        {
            _animatedRoot = _animancer.transform;
        }

        if (_animatedRoot != null)
        {
            _initialAnimatedRootLocalPosition = _animatedRoot.localPosition;
            _initialAnimatedRootLocalRotation = _animatedRoot.localRotation;
        }
    }

    private void Start()
    {
        _speedSmoothedParameter = new(_animancer, _speedParameter, 0.1f);
    }

    private void OnAnimatorMove()
    {
        if (_locomotionContext == null || !_locomotionContext.UseRootMotion)
        {
            return;
        }

        _locomotionContext.DeltaPosition += _animancer.Animator.deltaPosition;
        _locomotionContext.DeltaRotation = _animancer.Animator.deltaRotation * _locomotionContext.DeltaRotation;
    }

    public void SetLocomotionContext(LocomotionContext locomotionContext)
    {
        _locomotionContext = locomotionContext;
    }

    public void UpdateAnimation(LocomotionStateId locomotionState)
    {
        if (_locomotionContext.GroundedStateId == GroundedStateId.Dash
        ||  _locomotionContext.GroundedStateId == GroundedStateId.SprintImpulse
        ||  _locomotionContext.GroundedStateId == GroundedStateId.MoveStop)
        {
            return;
        }
        if (_locomotionContext.IsStableOnGround)
        {
            if (_currentKey != CharacterAnimationKey.Move)
            {
                _currentKey = CharacterAnimationKey.Move;
                _currentState = _animancer.Play(_move, 0.2f, FadeMode.FixedDuration);
            }
            _speedSmoothedParameter.TargetValue = _locomotionContext.HorizontalSpeed.magnitude;
        }
    }

    private void PlayIfChanged(CharacterAnimationKey key, ClipTransition transition, float fadeDuration, bool notifyEnd=false)
    {
        if (_currentKey == key)
        {
            return;
        }

        _currentKey = key;

        _currentState = _animancer.Play(transition, fadeDuration, FadeMode.FixedDuration);
        _currentState.Time = 0.0f;

        var events = _currentState.Events(this);
        events.OnEnd = notifyEnd ? OnCurrentAnimationEnded : null;
    }

    private void PlayIfChanged(CharacterAnimationKey key, LinearMixerTransition transition, float fadeDuration, bool notifyEnd=false)
    {
        if (_currentKey == key)
        {
            return;
        }

        _currentKey = key;

        Debug.Log("transition:"+transition.Name);

        _currentState = _animancer.Play(transition, fadeDuration, FadeMode.FixedDuration);
        _currentState.Time = 0.0f;

        var events = _currentState.Events(this);
        events.OnEnd = notifyEnd ? OnCurrentAnimationEnded : null;
    }

    private void OnCurrentAnimationEnded()
    {
        if (AnimancerEvent.Current.State != _currentState)
        {
            return;
        }

        AnimationEnded?.Invoke(_currentKey);
    }

    public void Play(AnimationCommand command)
    {
        switch (command.Key)
        {
            case CharacterAnimationKey.Move:
                PlayIfChanged(command.Key, _move, command.FadeDuration, command.NotifyEnd);
                break;
            case CharacterAnimationKey.DashF:
                PlayIfChanged(command.Key, _dashF, command.FadeDuration, command.NotifyEnd);
                break;
            case CharacterAnimationKey.SprintImpulse:
                PlayIfChanged(command.Key, _sprintImpulse, command.FadeDuration, command.NotifyEnd);
                break;
            case CharacterAnimationKey.StopRunL:
                PlayIfChanged(command.Key, _stopRunL, command.FadeDuration, command.NotifyEnd);
                break;
            default:
                break;
        }
    }

}
