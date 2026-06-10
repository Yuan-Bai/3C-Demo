using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-300)]
public sealed class PlayerInputReader : MonoBehaviour
{
    private PlayerInputAction _inputAction;
    private InputAction _attackAction;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _moveSwitchAction;
    private InputAction _lookAction;
    private InputAction _zoomAction;
    private InputAction _dashAction;

    [Header("Runtime Debug")]
    [SerializeField] private Vector2 _moveAxis;
    [SerializeField] private Vector2 _lookAxis;
    [SerializeField] private float _zoomDelta;
    [SerializeField] private int _lastConsumedSequence;

    private int _nextSequence;

    public Vector2 MoveAxis => _moveAxis;
    public Vector2 LookAxis => _lookAxis;
    public float ZoomDelta => _zoomDelta;

    public bool JumpPressed { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool JumpHeld { get; private set; }

    public bool MoveSwitchPressed { get; private set; }
    public bool MoveSwitchReleased { get; private set; }
    public bool MoveSwitchHeld { get; private set; }

    public bool AttackPressed { get; private set; }
    public bool AttackReleased { get; private set; }
    public bool AttackHeld { get; private set; }

    public bool DashPressed { get; private set; }
    public bool DashReleased { get; private set; }
    public bool DashHeld { get; private set; }

    public CharacterInputFrame LastConsumedFrame { get; private set; }

    private void Awake()
    {
        _inputAction = new PlayerInputAction();

        var playerActions = _inputAction.Player;
        _attackAction = playerActions.Attack;
        _moveAction = playerActions.Move;
        _jumpAction = playerActions.Jump;
        _moveSwitchAction = playerActions.MoveSwitch;
        _lookAction = playerActions.Look;
        _zoomAction = playerActions.Zoom;
        _dashAction = playerActions.Dash;

        RegisterButtonCallbacks();
    }

    private void OnEnable()
    {
        _inputAction?.Enable();
    }

    private void OnDisable()
    {
        _inputAction?.Disable();
        ClearContinuousInputs();
        ClearFrameInputs();
        ClearHeldInputs();
    }

    private void Update()
    {
        ReadContinuousInputs();
    }

    public CharacterInputFrame PeekFrame()
    {
        ReadContinuousInputs();

        return new CharacterInputFrame(
            _nextSequence,
            _moveAxis,
            _lookAxis,
            _zoomDelta,
            JumpPressed,
            JumpReleased,
            JumpHeld,
            MoveSwitchPressed,
            MoveSwitchReleased,
            MoveSwitchHeld,
            AttackPressed,
            AttackReleased,
            AttackHeld,
            DashPressed,
            DashReleased,
            DashHeld);
    }

    public CharacterInputFrame ConsumeFrame()
    {
        CharacterInputFrame frame = PeekFrame();
        LastConsumedFrame = frame;
        _lastConsumedSequence = frame.Sequence;
        _nextSequence++;
        ClearFrameInputs();
        return frame;
    }

    private void OnDestroy()
    {
        UnregisterButtonCallbacks();

        _inputAction?.Dispose();
        _attackAction = null;
        _moveAction = null;
        _jumpAction = null;
        _moveSwitchAction = null;
        _lookAction = null;
        _zoomAction = null;
        _dashAction = null;
    }

    private void ReadContinuousInputs()
    {
        _moveAxis = _moveAction == null ? Vector2.zero : _moveAction.ReadValue<Vector2>();
        _lookAxis = _lookAction == null ? Vector2.zero : _lookAction.ReadValue<Vector2>();
        _zoomDelta = _zoomAction == null ? 0.0f : _zoomAction.ReadValue<Vector2>().y;
    }

    private void ClearFrameInputs()
    {
        JumpPressed = false;
        JumpReleased = false;
        MoveSwitchPressed = false;
        MoveSwitchReleased = false;
        AttackPressed = false;
        AttackReleased = false;
        DashPressed = false;
        DashReleased = false;
    }

    private void ClearContinuousInputs()
    {
        _moveAxis = Vector2.zero;
        _lookAxis = Vector2.zero;
        _zoomDelta = 0.0f;
    }

    private void ClearHeldInputs()
    {
        JumpHeld = false;
        MoveSwitchHeld = false;
        AttackHeld = false;
        DashHeld = false;
    }

    private void RegisterButtonCallbacks()
    {
        if (_jumpAction != null)
        {
            _jumpAction.started += OnJumpStarted;
            _jumpAction.canceled += OnJumpCanceled;
        }

        if (_moveSwitchAction != null)
        {
            _moveSwitchAction.started += OnMoveSwitchStarted;
            _moveSwitchAction.canceled += OnMoveSwitchCanceled;
        }

        if (_attackAction != null)
        {
            _attackAction.started += OnAttackStarted;
            _attackAction.canceled += OnAttackCanceled;
        }

        if (_dashAction != null)
        {
            _dashAction.started += OnDashStarted;
            _dashAction.canceled += OnDashCanceled;
        }
    }

    private void UnregisterButtonCallbacks()
    {
        if (_jumpAction != null)
        {
            _jumpAction.started -= OnJumpStarted;
            _jumpAction.canceled -= OnJumpCanceled;
        }

        if (_moveSwitchAction != null)
        {
            _moveSwitchAction.started -= OnMoveSwitchStarted;
            _moveSwitchAction.canceled -= OnMoveSwitchCanceled;
        }

        if (_attackAction != null)
        {
            _attackAction.started -= OnAttackStarted;
            _attackAction.canceled -= OnAttackCanceled;
        }

        if (_dashAction != null)
        {
            _dashAction.started -= OnDashStarted;
            _dashAction.canceled -= OnDashCanceled;
        }
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        JumpPressed = true;
        JumpHeld = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        JumpReleased = true;
        JumpHeld = false;
    }

    private void OnMoveSwitchStarted(InputAction.CallbackContext context)
    {
        MoveSwitchPressed = true;
        MoveSwitchHeld = true;
    }

    private void OnMoveSwitchCanceled(InputAction.CallbackContext context)
    {
        MoveSwitchReleased = true;
        MoveSwitchHeld = false;
    }

    private void OnAttackStarted(InputAction.CallbackContext context)
    {
        AttackPressed = true;
        AttackHeld = true;
    }

    private void OnAttackCanceled(InputAction.CallbackContext context)
    {
        AttackReleased = true;
        AttackHeld = false;
    }

    private void OnDashStarted(InputAction.CallbackContext context)
    {
        DashPressed = true;
        DashHeld = true;
    }

    private void OnDashCanceled(InputAction.CallbackContext context)
    {
        DashReleased = true;
        DashHeld = false;
    }
}

[System.Serializable]
public readonly struct CharacterInputFrame
{
    public CharacterInputFrame(
        int sequence,
        Vector2 moveAxis,
        Vector2 lookAxis,
        float zoomDelta,
        bool jumpPressed,
        bool jumpReleased,
        bool jumpHeld,
        bool moveSwitchPressed,
        bool moveSwitchReleased,
        bool moveSwitchHeld,
        bool attackPressed,
        bool attackReleased,
        bool attackHeld,
        bool dashPressed,
        bool dashReleased,
        bool dashHeld)
    {
        Sequence = sequence;
        MoveAxis = moveAxis;
        LookAxis = lookAxis;
        ZoomDelta = zoomDelta;
        JumpPressed = jumpPressed;
        JumpReleased = jumpReleased;
        JumpHeld = jumpHeld;
        MoveSwitchPressed = moveSwitchPressed;
        MoveSwitchReleased = moveSwitchReleased;
        MoveSwitchHeld = moveSwitchHeld;
        AttackPressed = attackPressed;
        AttackReleased = attackReleased;
        AttackHeld = attackHeld;
        DashPressed = dashPressed;
        DashReleased = dashReleased;
        DashHeld = dashHeld;
    }

    public int Sequence { get; }
    public Vector2 MoveAxis { get; }
    public Vector2 LookAxis { get; }
    public float ZoomDelta { get; }

    public bool JumpPressed { get; }
    public bool JumpReleased { get; }
    public bool JumpHeld { get; }

    public bool MoveSwitchPressed { get; }
    public bool MoveSwitchReleased { get; }
    public bool MoveSwitchHeld { get; }

    public bool AttackPressed { get; }
    public bool AttackReleased { get; }
    public bool AttackHeld { get; }

    public bool DashPressed { get; }
    public bool DashReleased { get; }
    public bool DashHeld { get; }

    public static CharacterInputFrame Empty { get; } = new CharacterInputFrame(
        0,
        Vector2.zero,
        Vector2.zero,
        0.0f,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false,
        false);
}
