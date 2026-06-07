using UnityEngine;

[DisallowMultipleComponent]
public sealed class MyCode : MonoBehaviour
{
    private const float MinMoveSqrMagnitude = 0.000001f;

    [Header("References")]
    [SerializeField] private PlayerGroundProbe _groundProbe;

    [Header("Collision")]
    [SerializeField] private LayerMask _collisionLayers = ~0;
    [SerializeField, Min(0.2f)] private float _capsuleHeight = 1.52f;
    [SerializeField, Min(0.05f)] private float _capsuleRadius = 0.15f;
    [SerializeField] private Vector3 _capsuleOffset = Vector3.zero;
    [SerializeField, Min(0.0001f)] private float _skinWidth = 0.01f;
    [SerializeField, Range(1, 5)] private int _maxSlideIterations = 3;

    [Header("Steps")]
    [SerializeField, Min(0.0f)] private float _maxStepHeight = 0.35f;
    [SerializeField, Min(0.0f)] private float _minStepHeight = 0.03f;
    [SerializeField, Min(0.0f)] private float _stepForwardOvershoot = 0.06f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;

    #region 供外部访问属性
    public Vector3 LastRequestedDisplacement { get; private set; }
    public Vector3 LastAppliedDisplacement { get; private set; }
    public Vector3 LastHitNormal { get; private set; }
    public bool HitSomething { get; private set; }
    public PlayerGroundHit GroundHit { get; private set; }
    public bool IsGrounded => GroundHit.HasHit;
    public bool IsOnWalkableGround => GroundHit.HasHit && GroundHit.IsWalkable;
    public Vector3 GroundPoint => GroundHit.Point;
    public Vector3 GroundNormal => IsGrounded ? GroundHit.Normal : Vector3.up;
    public float GroundSlopeAngle => GroundHit.SlopeAngle;
    public Vector3 LastVelocity { get; private set; }
    public Vector3 LastHorizontalVelocity { get; private set; }
    public bool SteppedUp { get; private set; }
    public bool SnappedToGround { get; private set; }
    public float LastStepHeight { get; private set; }
    public LayerMask CollisionLayers => _collisionLayers;
    #endregion

    private void Awake()
    {
        _groundProbe??=GetComponent<PlayerGroundProbe>();
    }

    public Vector3 Move(Vector3 displacement)
    {
        LastRequestedDisplacement = displacement;
        LastAppliedDisplacement = Vector3.zero;
        LastHitNormal = Vector3.zero;
        HitSomething = false;
        SteppedUp = false;
        SnappedToGround = false;
        LastStepHeight = 0.0f;

        Vector3 adjustedDisplacement = AdjustForGround(displacement);
        return Vector3.zero;
    }

    private Vector3 AdjustForGround(Vector3 displacement)
    {
        if (!IsOnWalkableGround)
        {
            return displacement;
        }

        // 去除包含跳跃、重力、下落等 y 分量，用于计算slopeDirection
        Vector3 horizontal = Vector3.ProjectOnPlane(displacement, Vector3.up);
        if (horizontal.sqrMagnitude <= MinMoveSqrMagnitude)
        {
            return displacement;
        }

        Vector3 slopeDirection = Vector3.ProjectOnPlane(horizontal, GroundNormal);
        if (slopeDirection.sqrMagnitude <= MinMoveSqrMagnitude)
        {
            return displacement;
        }

        Vector3 vertical = Vector3.up * displacement.y;
        return slopeDirection.normalized * horizontal.magnitude + vertical;
    }
}