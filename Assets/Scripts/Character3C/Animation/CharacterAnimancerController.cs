using System.Collections;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

public class CharacterAnimancerController : MonoBehaviour
{
    [SerializeField] private AnimancerComponent _animancer;

    [SerializeField] private ClipTransition _idle;
    [SerializeField] private ClipTransition _walk;
    [SerializeField] private ClipTransition _run;
    [SerializeField] private ClipTransition _sprint;

    [SerializeField] private ClipTransition _dash;
    [SerializeField] private ClipTransition _sprintImpulse;
    [SerializeField] private ClipTransition _moveStop;
    
    [SerializeField] private ClipTransition _jump;
    [SerializeField] private ClipTransition _jumpSecond;
    [SerializeField] private ClipTransition _fall;

    private object _currentKey;

    public void UpdateAnimation(LocomotionContext context)
    {
        
    }
}
