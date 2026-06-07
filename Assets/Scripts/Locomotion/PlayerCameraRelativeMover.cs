using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerKinematicMotor))]
public sealed class PlayerCameraRelativeMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader _input;
    [SerializeField] private PlayerKinematicMotor _motor;
    [SerializeField] private PlayerGroundProbe _groundProbe;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _rotationRoot;

    [Header("Movement")]
    [SerializeField, Min(0.0f)] private float _moveSpeed = 4.0f;
    [SerializeField, Min(0.0f)] private float _rotationSharpness = 12.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float _moveDeadZone = 0.1f;

    public bool HasMoveInput { get; private set; }
    public Vector3 MoveDirection { get; private set; }

    private void Awake()
    {
        _input ??= GetComponent<PlayerInputReader>();
        _motor ??= GetComponent<PlayerKinematicMotor>();
        _groundProbe ??= GetComponent<PlayerGroundProbe>();
        _rotationRoot ??= transform;
        _cameraTransform ??= Camera.main.transform;
    }

    private void Update()
    {
        if (_input == null)
        {
            HasMoveInput = false;
            MoveDirection = Vector3.zero;
            return;
        }

        // 进行一次地面检测，不交给GroundProbe.update每帧执行，方便固定顺序
        _groundProbe.ProbeGround();

        // _motor.SnapToGround();

        // 斜向同时按下 W+D 时，输入长度会超过 1；限制长度可以避免斜向移动比正向更快。
        Vector2 moveAxis = Vector2.ClampMagnitude(_input.MoveAxis, 1.0f);
        HasMoveInput = moveAxis.sqrMagnitude > _moveDeadZone * _moveDeadZone;

        if (!HasMoveInput)
        {
            MoveDirection = Vector3.zero;
            return;
        }

        MoveDirection = GetCameraRelativeDirection(moveAxis);
        Vector3 displacement = MoveDirection * (_moveSpeed * Time.deltaTime);

        // 进行位移
        _motor.Move(displacement);
        // 进行转向
        RotateToward(MoveDirection);
    }

    private Vector3 GetCameraRelativeDirection(Vector2 moveAxis)
    {
        Transform reference = _cameraTransform == null ? transform : _cameraTransform;

        // 第三人称移动通常只关心水平面方向，所以要去掉相机 forward/right 的 Y 分量。
        // 否则相机向下看时，按 W 会把角色往地面方向推。
        Vector3 forward = ProjectOnHorizontalPlane(reference.forward, transform.forward);
        Vector3 right = ProjectOnHorizontalPlane(reference.right, transform.right);

        Vector3 direction = forward * moveAxis.y + right * moveAxis.x;
        return direction.sqrMagnitude > 1.0f ? direction.normalized : direction;
    }

    private void RotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        // 指数插值比固定比例插值更稳定：不同帧率下转向手感更接近。
        float blend = 1.0f - Mathf.Exp(-_rotationSharpness * Time.deltaTime);
        _rotationRoot.rotation = Quaternion.Slerp(_rotationRoot.rotation, targetRotation, blend);
    }

    private static Vector3 ProjectOnHorizontalPlane(Vector3 direction, Vector3 fallback)
    {
        direction.y = 0.0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            return direction.normalized;
        }

        fallback.y = 0.0f;
        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
    }
}
