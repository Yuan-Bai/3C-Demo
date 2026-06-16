using System;
using UnityEngine;

public sealed class CharacterCoordinator : MonoBehaviour
{
    private HierarchicalStateMachine _stateMachine = new();
    private CharacterContext _ctx;
    private IKccMotorPort _motor;
    private IAnimancerPort _anim;
    private ICharacterEventBus _bus;
    private CharacterCommandBuffer _command;
    [SerializeField] private CharacterBlackboard _bb;
    private RootMotionAccumulator _motionAccumulator;
    private Transform camTransform;
    private IDisposable _stateRequestSub;
    private IDisposable _stateEndedSub;

    [SerializeField] private PlayerInputReader _input;
    [SerializeField] private AnimationStateDatabase _defs;

    public ICharacterState CurrentState => _stateMachine.CurrentState;

    private void Awake()
    {
        // Coordinator 是角色逻辑的唯一入口：
        // 1. 装配 KCC/Animancer 端口和共享 Context。
        // 2. 注册所有状态实例。
        // 3. 订阅 Bus 事件并统一执行状态机切换。
        _motor ??= GetComponent<KccCharacterAdapter>();
        var animationController = GetComponent<MyAnimationController>();
        _anim ??= animationController;
        _bus ??= new CharacterEventBus();
        _command ??= new CharacterCommandBuffer();
        _bb ??= new CharacterBlackboard();
        _input ??= GetComponent<PlayerInputReader>();
        _defs ??= Resources.Load<AnimationStateDatabase>("QianXiao/Animations/AnimationStateDatabase");
        _motionAccumulator ??= GetComponentInChildren<RootMotionAccumulator>();

        camTransform ??= Camera.main.transform;

        if (_defs == null)
        {
            Debug.LogWarning("CharacterDefinitions asset is not assigned. Runtime defaults will be used.", this);
            _defs = ScriptableObject.CreateInstance<AnimationStateDatabase>();
        }

        animationController?.Initialize(_defs);

        _ctx = new CharacterContext(_motor, _anim, _bus, _command, _bb, _defs, _motionAccumulator);
        RegisterStates();

        _stateMachine.ForceChange(new StateChangeRequest(
            CharacterStateId.Idle,
            CharacterStateId.Idle,
            StatePriority.Locomotion,
            "initial state"));
    }

    private void Start()
    {
        Animancer.Editor.AnimationGatherer.logExceptions = true;       
    }

    private void OnEnable()
    {
        if (_bus == null)
        {
            return;
        }

        _stateRequestSub = _bus.Subscribe<CharacterStateChangeRequestedEvent>(OnStateChangeRequested);
        _stateEndedSub = _bus.Subscribe<CharacterStateEndedEvent>(OnStateEnded);
    }

    private void OnDisable()
    {
        _stateRequestSub?.Dispose();
        _stateRequestSub = null;
        _stateEndedSub?.Dispose();
        _stateEndedSub = null;
    }

    private void Update()
    {
        // Update 和 KCC 的 FixedUpdate 模拟不是 1:1：
        // 一帧可能没有 KCC tick，也可能有多个 KCC tick。
        // 所以这里仅缓存输入与一次性命令；状态决策放到 KCC 的 BeforeCharacterUpdate 中执行。
        _ctx.FrameDeltaTime = Time.deltaTime;
    }

    #region KCC部分周期函数
    public void BeforeCharacterUpdate(float deltaTime)
    {
        _ctx.TickDeltaTime = deltaTime;
        ReadInputIntoBlackboard();

        // 连续状态判断放在 KCC tick 中，保证本 tick 的 UpdateVelocity/UpdateRotation
        // 使用的是同一次状态决策结果。
        // _stateMachine.CurrentState?.Tick(deltaTime);
        _stateMachine.CurrentState?.BeforeCharacterUpdate(deltaTime);

        ConsumeBufferedCommands();
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        // KCC 在这里刚完成地面探测，grounded/normal 在这个点之后才是本 tick 的最新值。
        _stateMachine.CurrentState?.PostGroundingUpdate(deltaTime);
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        _stateMachine.CurrentState?.AfterCharacterUpdate(deltaTime);
    }
    #endregion

    private void RegisterStates()
    {
        _stateMachine.AddState(new IdleState(_ctx));
        _stateMachine.AddState(new MoveState(_ctx));
        _stateMachine.AddState(new MoveStopState(_ctx));
        _stateMachine.AddState(new DashState(_ctx));
    }

    private void ReadInputIntoBlackboard()
    {
        CharacterInputFrame frame = _input != null ? _input.ConsumeFrame() : CharacterInputFrame.Empty;

        _ctx.Bb.InputFrame = frame;

        Vector3 moveInput = Vector3.ClampMagnitude(new Vector3(frame.MoveAxis.x, 0.0f, frame.MoveAxis.y), 1.0f);

        if (!_ctx.Defs.HasMoveInput(moveInput))
        {
            _ctx.Bb.HasMoveInput = false;
            return;
        }
        _ctx.Bb.HasMoveInput = true;
        
        Vector3 camPlanarForward = Vector3.ProjectOnPlane(camTransform.forward, _motor.CharacterUp).normalized;
        Quaternion camPlanarRotation = Quaternion.LookRotation(camPlanarForward, _motor.CharacterUp);

        _ctx.Bb.LookDirection = camPlanarForward;
        _ctx.Bb.MoveDirection = camPlanarRotation * moveInput;

        if (_ctx.Bb.MoveDirection.sqrMagnitude > 0.0001f)
        {
            _ctx.Bb.Facing = _ctx.Bb.MoveDirection.normalized;
        }
    }

    private void ConsumeBufferedCommands()
    {
        if (_ctx.Commands.TryConsume(CommandChannel.Action, Time.time, out var command) &&
            command.Type == CharacterCommandType.Dash)
        {
            if (TryChangeState(CharacterStateId.Dash, StatePriority.Dash, command.Reason))
            {
                return;
            }
            else
            {
                _command.Push(command, Time.time);
                Debug.Log("push");
                return;
            }
        }

        if (_ctx.Commands.TryConsume(CommandChannel.Locomotion, Time.time, out command))
        {
            if (command.Type == CharacterCommandType.Movestop)
            {
                TryChangeState(CharacterStateId.MoveStop, StatePriority.Locomotion, command.Reason);
                return;
            }
        }
    }

    private void OnStateChangeRequested(CharacterStateChangeRequestedEvent evt)
    {
        if (_stateMachine.CurrentState != null && _stateMachine.CurrentStateId != evt.Source)
        {
            return;
        }

        TryChangeState(evt.Target, evt.Priority, evt.Reason);
    }

    private void OnStateEnded(CharacterStateEndedEvent evt)
    {
        if (_stateMachine.CurrentState == null || _stateMachine.CurrentStateId != evt.Source)
        {
            return;
        }

        TryChangeState(ResolvePostActionState(), StatePriority.Locomotion, evt.Reason);
    }

    private void RequestState(CharacterStateId target, StatePriority priority, string reason)
    {
        _ctx.Bus.Publish(new CharacterStateChangeRequestedEvent(
            _stateMachine.CurrentStateId,
            target,
            priority,
            reason));
    }

    private bool TryChangeState(CharacterStateId target, StatePriority priority, string reason)
    {
        var request = new StateChangeRequest(
            _stateMachine.CurrentStateId,
            target,
            priority,
            reason);

        return _stateMachine.ForceChange(request);
    }

    private CharacterStateId ResolvePostActionState()
    {
        if (!_ctx.Motor.IsGrounded)
        {
            return _ctx.Motor.Velocity.y > 0.0f ? CharacterStateId.Rise : CharacterStateId.Fall;
        }

        if (_ctx.Bb.MoveDirection.sqrMagnitude > 0.0001f)
        {
            return CharacterStateId.Move;
        }

        return CharacterStateId.Idle;
    }
}
