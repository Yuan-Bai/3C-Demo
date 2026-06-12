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
    private CharacterBlackboard _bb;
    private IDisposable _stateRequestSub;
    private IDisposable _stateEndedSub;

    [SerializeField] private PlayerInputReader _input;
    [SerializeField] private Transform _moveReference = null;
    [SerializeField] private CharacterDefinitions _defs;

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
        _defs ??= Resources.Load<CharacterDefinitions>("QianXiao/Animations/CharacterDefinitions");

        if (_defs == null)
        {
            Debug.LogWarning("CharacterDefinitions asset is not assigned. Runtime defaults will be used.", this);
            _defs = ScriptableObject.CreateInstance<CharacterDefinitions>();
        }

        animationController?.Initialize(_defs);

        _ctx = new CharacterContext(_motor, _anim, _bus, _command, _bb, _defs);
        RegisterStates();

        _stateMachine.ForceChange(new StateChangeRequest(
            CharacterStateId.Idle,
            CharacterStateId.Idle,
            StatePriority.Locomotion,
            "initial state"));
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
        if (_ctx == null)
        {
            return;
        }

        // Update 和 KCC 的 FixedUpdate 模拟不是 1:1：
        // 一帧可能没有 KCC tick，也可能有多个 KCC tick。
        // 所以这里仅缓存输入与一次性命令；状态决策放到 KCC 的 BeforeCharacterUpdate 中执行。
        _ctx.FrameDeltaTime = Time.deltaTime;
        ReadInputIntoBlackboard();
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        if (_ctx == null)
        {
            return;
        }

        _ctx.TickDeltaTime = deltaTime;
        SyncMotorIntoBlackboard();
        ConsumeBufferedCommands();

        // 连续状态判断放在 KCC tick 中，保证本 tick 的 UpdateVelocity/UpdateRotation
        // 使用的是同一次状态决策结果。
        _stateMachine.CurrentState?.Tick(deltaTime);
        _stateMachine.CurrentState?.BeforeCharacterUpdate(deltaTime);
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        if (_ctx == null)
        {
            return;
        }

        // KCC 在这里刚完成地面探测，grounded/normal 在这个点之后才是本 tick 的最新值。
        SyncMotorIntoBlackboard();
        _stateMachine.CurrentState?.PostGroundingUpdate(deltaTime);
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        if (_ctx == null)
        {
            return;
        }

        _stateMachine.CurrentState?.AfterCharacterUpdate(deltaTime);
        SyncMotorIntoBlackboard();
    }

    private void RegisterStates()
    {
        _stateMachine.AddState(new IdleState(_ctx));
        _stateMachine.AddState(new MoveState(_ctx));
        _stateMachine.AddState(new MoveStopState(_ctx));
        _stateMachine.AddState(new DashState(_ctx));
    }

    private void ReadInputIntoBlackboard()
    {
        var frame = _input != null ? _input.ConsumeFrame() : CharacterInputFrame.Empty;

        _ctx.Bb.MoveInput = Vector2.ClampMagnitude(frame.MoveAxis, 1.0f);
        _ctx.Bb.DesiredWorldMove = ResolveDesiredWorldMove(_ctx.Bb.MoveInput);

        if (_ctx.Bb.DesiredWorldMove.sqrMagnitude > 0.0001f)
        {
            _ctx.Bb.Facing = _ctx.Bb.DesiredWorldMove.normalized;
        }

        if (frame.DashPressed)
        {
            float now = Time.time;
            _ctx.Commands.Push(new CharacterCommand(
                CharacterCommandType.Dash,
                CommandChannel.Action,
                (int)StatePriority.Dash,
                now + _ctx.Defs.DashCommandBufferTime,
                "dash pressed"), now);
        }
    }

    private Vector3 ResolveDesiredWorldMove(Vector2 moveInput)
    {
        if (!_ctx.Defs.HasMoveInput(moveInput))
        {
            return Vector3.zero;
        }

        var reference = _moveReference != null ? _moveReference : transform;
        var up = _motor != null ? _motor.CharacterUp : Vector3.up;
        var forward = Vector3.ProjectOnPlane(reference.forward, up).normalized;
        var right = Vector3.ProjectOnPlane(reference.right, up).normalized;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.right;
        }

        return Vector3.ClampMagnitude(right * moveInput.x + forward * moveInput.y, 1.0f);
    }

    private void SyncMotorIntoBlackboard()
    {
        if (_motor == null)
        {
            return;
        }

        _ctx.Bb.IsGrounded = _motor.IsGrounded;
        _ctx.Bb.GroundNormal = _motor.GroundNormal;
        _ctx.Bb.VerticalSpeed = Vector3.Dot(_motor.Velocity, _motor.CharacterUp);
    }

    private void ConsumeBufferedCommands()
    {
        if (_ctx.Commands.TryConsume(CommandChannel.Action, Time.time, out var command) &&
            command.Type == CharacterCommandType.Dash)
        {
            RequestState(CharacterStateId.Dash, StatePriority.Dash, command.Reason);
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

        return _stateMachine.TryChange(request);
    }

    private CharacterStateId ResolvePostActionState()
    {
        if (!_ctx.Bb.IsGrounded)
        {
            return _ctx.Bb.VerticalSpeed > 0.0f ? CharacterStateId.Rise : CharacterStateId.Fall;
        }

        if (_ctx.Bb.DesiredWorldMove.sqrMagnitude > 0.0001f)
        {
            return CharacterStateId.Move;
        }

        return CharacterStateId.Idle;
    }
}
