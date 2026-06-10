using Animancer;
using UnityEngine;

public class CharacterAnimancerController : MonoBehaviour
{
    [SerializeField] private AnimancerComponent _animancer;
    [SerializeField] private bool _disableRootMotion = true;
    [SerializeField] private bool _lockAnimatedRootToInitialLocalPose;
    [SerializeField] private Transform _animatedRoot;

    [SerializeField] private ClipTransition _idle;
    [SerializeField] private ClipTransition _walk;
    [SerializeField] private ClipTransition _run;
    [SerializeField] private ClipTransition _sprint;

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

    private CharacterAnimationKey _currentKey;
    private AnimancerState _currentState;
    private Vector3 _initialAnimatedRootLocalPosition;
    private Quaternion _initialAnimatedRootLocalRotation;

    public event System.Action<CharacterAnimationKey> AnimationEnded;

    private void Awake()
    {
        _animancer ??= GetComponent<AnimancerComponent>();

        if (_animancer != null && _animancer.Animator != null && _disableRootMotion)
        {
            _animancer.Animator.applyRootMotion = false;
        }

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

    private void LateUpdate()
    {
        if (!_lockAnimatedRootToInitialLocalPose || _animatedRoot == null)
        {
            return;
        }

        _animatedRoot.localPosition = _initialAnimatedRootLocalPosition;
        _animatedRoot.localRotation = _initialAnimatedRootLocalRotation;
    }

    public void UpdateAnimation(LocomotionStateId locomotionState, LocomotionContext context)
    {
        if (locomotionState == LocomotionStateId.Grounded)
        {
            switch (context.GroundedStateId)
            {
                case GroundedStateId.Idle:
                    PlayIfChanged(CharacterAnimationKey.Idle, _idle, false);
                    break;

                case GroundedStateId.Walk:
                    PlayIfChanged(CharacterAnimationKey.Walk, _walk, false);
                    break;

                case GroundedStateId.Run:
                    PlayIfChanged(CharacterAnimationKey.Run, _run, false);
                    break;

                case GroundedStateId.Sprint:
                    PlayIfChanged(CharacterAnimationKey.Sprint, _sprint, false);
                    break;

                case GroundedStateId.Dash:
                    PlayIfChanged(CharacterAnimationKey.DashF, _dashF, true);
                    break;

                case GroundedStateId.SprintImpulse:
                    PlayIfChanged(CharacterAnimationKey.SprintImpulse, _sprintImpulse, true);
                    break;

                case GroundedStateId.MoveStop:
                    PlayIfChanged(CharacterAnimationKey.StopRunL, _stopRunL, true);
                    break;
            }

            return;
        }

        if (locomotionState == LocomotionStateId.Airborne)
        {
            if (context.AirborneAction == AirborneActionId.Jump)
            {
                PlayIfChanged(CharacterAnimationKey.JumpRunL, _jumpRunL, true);
                return;
            }

            if (context.AirborneAction == AirborneActionId.JumpSecond)
            {
                PlayIfChanged(CharacterAnimationKey.JumpSecondF, _jumpSecondF, true);
                return;
            }

            PlayIfChanged(CharacterAnimationKey.Fall, _fall, false);
        }
    }

    private void PlayIfChanged(CharacterAnimationKey key, ClipTransition transition, bool notifyEnd)
    {
        if (_animancer == null || transition == null)
        {
            return;
        }

        if (_currentKey == key)
        {
            return;
        }

        _currentKey = key;

        _currentState = _animancer.Play(transition, 0.0f);
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
}
