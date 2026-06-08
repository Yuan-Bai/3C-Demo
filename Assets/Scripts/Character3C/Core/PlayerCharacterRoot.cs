using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacterRoot : MonoBehaviour
{
    [Header("References")]
    private KccCharacterController _controller;

    private readonly StateMachine<LocomotionStateId> _locomotionStateMachine = new();
    public Context _context = new();

    private void Awake()
    {
        _controller ??= GetComponent<KccCharacterController>();
        _controller.context = _context;
    }

    private void Start()
    {
        _locomotionStateMachine.AddState(new GroundedState(LocomotionStateId.Grounded, _locomotionStateMachine, _context));
        _locomotionStateMachine.AddState(new AirborneState(LocomotionStateId.Airborne, _locomotionStateMachine, _context));
        _locomotionStateMachine.AddState(new ClimbingState(LocomotionStateId.Climbing, _locomotionStateMachine, _context));
        _locomotionStateMachine.AddState(new LandingState(LocomotionStateId.Landing, _locomotionStateMachine, _context));
    
        _locomotionStateMachine.ChangeState(LocomotionStateId.Grounded);
    }

    private void Update()
    {
        _locomotionStateMachine.Tick(Time.deltaTime);
    }
}
