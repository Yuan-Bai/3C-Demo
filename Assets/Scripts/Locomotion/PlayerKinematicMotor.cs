using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class PlayerKinematicMotor : MonoBehaviour
{
    private const float MinMoveSqrMagnitude = 0.000001f;
    private const int MaxOverlapResults = 16;
    private const int MaxDepenetrationIterations = 4;
    private const float DepenetrationPadding = 0.001f;

    private readonly Collider[] _overlapResults = new Collider[MaxOverlapResults];
    private CapsuleCollider _depenetrationCapsule;

    [Header("References")]
    [SerializeField] private PlayerGroundProbe _groundProbe;

    [Header("Collision")]
    [SerializeField] private LayerMask _collisionLayers = ~0;
    [SerializeField, Min(0.2f)] private float _capsuleHeight = 1.6f;
    [SerializeField, Min(0.05f)] private float _capsuleRadius = 0.35f;
    [SerializeField, Min(0.0f)] private float _skinWidth = 0.03f;
    [SerializeField, Range(1, 5)] private int _maxSlideIterations = 3;

    [Header("Grounding")]
    [SerializeField, Range(0.0f, 89.0f)] private float _maxWalkableSlope = 45.0f;
    [SerializeField, Min(0.01f)] private float _groundProbeRadius = 0.25f;
    [SerializeField, Min(0.0f)] private float _groundProbeStartHeight = 0.6f;
    [SerializeField, Min(0.01f)] private float _groundProbeDistance = 1.0f;
    [SerializeField, Min(0.0f)] private float _groundSnapDistance = 0.45f;

    [Header("Steps")]
    [SerializeField, Min(0.0f)] private float _maxStepHeight = 0.35f;
    [SerializeField, Min(0.0f)] private float _minStepHeight = 0.03f;
    [SerializeField, Min(0.0f)] private float _stepForwardOvershoot = 0.06f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;

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
    public float CapsuleHeight => _capsuleHeight;
    public float CapsuleRadius => _capsuleRadius;
    public float SkinWidth => _skinWidth;
    public float MaxWalkableSlope => GetMaxWalkableSlope();
    public float GroundProbeRadius => GetProbeRadius();
    public float GroundProbeStartHeight => GetProbeStartHeight();
    public float GroundProbeDistance => GetProbeDistance();
    public float GroundSnapDistance => _groundSnapDistance;
    public float MaxStepHeight => _maxStepHeight;
    public float MinStepHeight => _minStepHeight;

    private void Reset()
    {
        _groundProbe = GetComponent<PlayerGroundProbe>();
    }

    private void Awake()
    {
        if (_groundProbe == null)
        {
            _groundProbe = GetComponent<PlayerGroundProbe>();
        }
    }

    private void OnDestroy()
    {
        if (_depenetrationCapsule == null)
        {
            return;
        }

        GameObject helper = _depenetrationCapsule.gameObject;
        _depenetrationCapsule = null;

        if (Application.isPlaying)
        {
            Destroy(helper);
        }
        else
        {
            DestroyImmediate(helper);
        }
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

        RefreshGround();
        bool wasOnWalkableGround = IsOnWalkableGround;

        Vector3 adjustedDisplacement = AdjustForGround(displacement);
        MoveWithCollision(adjustedDisplacement);

        if (wasOnWalkableGround || adjustedDisplacement.y <= 0.0f)
        {
            SnapToGround();
        }

        RefreshGround();

        float deltaTime = Time.deltaTime;
        LastVelocity = deltaTime > 0.0f ? LastAppliedDisplacement / deltaTime : Vector3.zero;
        LastHorizontalVelocity = Vector3.ProjectOnPlane(LastVelocity, Vector3.up);

        return LastAppliedDisplacement;
    }

    public bool TryProbeGroundAt(Vector3 worldPosition, float startHeight, float distance, out PlayerGroundHit hit)
    {
        if (_groundProbe != null)
        {
            return _groundProbe.TryProbeGroundAt(worldPosition, startHeight, distance, out hit);
        }

        return ProbeGroundAt(worldPosition, startHeight, distance, out hit);
    }

    public bool TryProbeGroundAt(Vector3 worldPosition, out PlayerGroundHit hit)
    {
        return TryProbeGroundAt(worldPosition, GetProbeStartHeight(), GetProbeDistance(), out hit);
    }

    public bool TryGetGroundPose(
        Vector3 worldPosition,
        Vector3 forwardHint,
        float startHeight,
        float distance,
        out Pose pose)
    {
        if (TryProbeGroundAt(worldPosition, startHeight, distance, out PlayerGroundHit hit))
        {
            pose = hit.GetSurfacePose(forwardHint);
            return true;
        }

        pose = PlayerGroundHit.CreateFallbackPose(worldPosition, forwardHint);
        return false;
    }

    public bool TryGetGroundPose(Vector3 worldPosition, Vector3 forwardHint, out Pose pose)
    {
        return TryGetGroundPose(worldPosition, forwardHint, GetProbeStartHeight(), GetProbeDistance(), out pose);
    }

    private Vector3 AdjustForGround(Vector3 displacement)
    {
        if (!IsOnWalkableGround)
        {
            return displacement;
        }

        // 去除包含跳跃、重力、下落等 y 分量
        Vector3 horizontal = Vector3.ProjectOnPlane(displacement, Vector3.up);
        if (horizontal.sqrMagnitude <= MinMoveSqrMagnitude)
        {
            return displacement;
        }

        Vector3 slopeDirection = Vector3.ProjectOnPlane(horizontal, GroundHit.Normal);
        if (slopeDirection.sqrMagnitude <= MinMoveSqrMagnitude)
        {
            return displacement;
        }

        Vector3 vertical = Vector3.up * displacement.y;
        return slopeDirection.normalized * horizontal.magnitude + vertical;
    }

    private Vector3 hitPoint1 = Vector3.zero;
    private void MoveWithCollision(Vector3 displacement)
    {
        ResolveBodyOverlaps();

        Vector3 remaining = displacement;

        for (int i = 0; i < _maxSlideIterations; i++)
        {
            if (remaining.sqrMagnitude <= MinMoveSqrMagnitude)
            {
                break;
            }

            Vector3 direction = remaining.normalized;
            float distance = remaining.magnitude;

            bool hasHit = CastBody(direction, distance + _skinWidth, out RaycastHit hit);
            if (!hasHit)
            {
                ApplyPositionDelta(remaining);
                ResolveBodyOverlaps();
                break;
            }

            hitPoint1 = hit.point;

            // 如果打中地面，通过判断可以向前移动，但是最后skinwidth不为0，以避免一直打中地面导致穿墙
            if (IsWalkableSurface(hit.normal))
            {
                Debug.Log(hit.collider.gameObject.name);
                ApplyPositionDelta(remaining);
                ResolveBodyOverlaps();
                break;
            }

            HitSomething = true;
            LastHitNormal = hit.normal;

            // Keep a small gap so floating point error does not wedge the capsule into the wall.
            float safeDistance = Mathf.Max(0.0f, hit.distance - _skinWidth);
            Vector3 safeMove = direction * safeDistance;
            ApplyPositionDelta(safeMove);
            ResolveBodyOverlaps();

            Vector3 leftover = direction * Mathf.Max(0.0f, distance - safeDistance);

            // if (TryStepUp(leftover, out Vector3 stepDelta))
            // {
            //     ApplyPositionDelta(stepDelta);
            //     break;
            // }

            remaining = Vector3.ProjectOnPlane(leftover, hit.normal);
            if (hit.normal.y > 0.0f && remaining.y > 0.0f)
            {
                remaining.y = 0.0f;
            }
        }
    }

    private bool CastBody(Vector3 direction, float distance, out RaycastHit hit)
    {
        return CastBody(transform.position, direction, distance, out hit);
    }

    private bool CastBody(Vector3 position, Vector3 direction, float distance, out RaycastHit hit)
    {
        GetCapsulePoints(position, out Vector3 bottom, out Vector3 top);
        return Physics.CapsuleCast(
            bottom,
            top,
            _capsuleRadius,
            direction,
            out hit,
            distance,
            _collisionLayers,
            QueryTriggerInteraction.Ignore);
    }

    private bool ResolveBodyOverlaps()
    {
        bool moved = false;

        for (int i = 0; i < MaxDepenetrationIterations; i++)
        {
            if (!TryResolveSingleBodyOverlap())
            {
                break;
            }

            moved = true;
        }

        return moved;
    }

    private bool TryResolveSingleBodyOverlap()
    {
        GetCapsulePoints(out Vector3 bottom, out Vector3 top);
        int count = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            _capsuleRadius,
            _overlapResults,
            _collisionLayers,
            QueryTriggerInteraction.Ignore);

        if (count <= 0)
        {
            return false;
        }

        CapsuleCollider bodyCollider = GetDepenetrationCapsule();
        ConfigureDepenetrationCapsule(bodyCollider, transform.position);

        for (int i = 0; i < count; i++)
        {
            Collider other = _overlapResults[i];
            _overlapResults[i] = null;

            if (ShouldIgnoreOverlapCollider(other))
            {
                continue;
            }

            bodyCollider.enabled = true;
            bool hasPenetration = Physics.ComputePenetration(
                bodyCollider,
                transform.position,
                Quaternion.identity,
                other,
                other.transform.position,
                other.transform.rotation,
                out Vector3 direction,
                out float distance);
            bodyCollider.enabled = false;

            if (!hasPenetration || distance <= 0.0f)
            {
                continue;
            }

            Vector3 correction = direction.normalized * (distance + DepenetrationPadding);
            correction = ConstrainDepenetrationCorrection(other, correction);
            if (correction.sqrMagnitude <= MinMoveSqrMagnitude)
            {
                continue;
            }

            ApplyPositionDelta(correction);
            return true;
        }

        bodyCollider.enabled = false;
        return false;
    }

    private CapsuleCollider GetDepenetrationCapsule()
    {
        if (_depenetrationCapsule != null)
        {
            return _depenetrationCapsule;
        }

        GameObject helper = new GameObject("PlayerKinematicMotor Depenetration Capsule")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        _depenetrationCapsule = helper.AddComponent<CapsuleCollider>();
        _depenetrationCapsule.direction = 1;
        _depenetrationCapsule.enabled = false;
        return _depenetrationCapsule;
    }

    private void ConfigureDepenetrationCapsule(CapsuleCollider capsule, Vector3 position)
    {
        float radius = Mathf.Max(0.01f, _capsuleRadius);
        float height = Mathf.Max(_capsuleHeight, radius * 2.0f);

        capsule.radius = radius;
        capsule.height = height;
        capsule.center = Vector3.up * (height * 0.5f);
        capsule.transform.SetPositionAndRotation(position, Quaternion.identity);
    }

    private bool ShouldIgnoreOverlapCollider(Collider other)
    {
        if (other == null || other == _depenetrationCapsule)
        {
            return true;
        }

        Transform otherTransform = other.transform;
        return otherTransform == transform || otherTransform.IsChildOf(transform);
    }

    private Vector3 ConstrainDepenetrationCorrection(Collider other, Vector3 correction)
    {
        if (correction.y <= _skinWidth)
        {
            return correction;
        }

        float footY = transform.position.y;
        if (other.bounds.max.y <= footY + _skinWidth * 2.0f)
        {
            return correction;
        }

        Vector3 horizontal = Vector3.ProjectOnPlane(correction, Vector3.up);
        if (horizontal.sqrMagnitude > MinMoveSqrMagnitude)
        {
            return horizontal;
        }

        Vector3 away = transform.position - other.bounds.center;
        away.y = 0.0f;
        return away.sqrMagnitude > MinMoveSqrMagnitude
            ? away.normalized * correction.magnitude
            : Vector3.zero;
    }

    private bool TryStepUp(Vector3 movement, out Vector3 stepDelta)
    {
        stepDelta = Vector3.zero;

        Vector3 horizontal = Vector3.ProjectOnPlane(movement, Vector3.up);
        if (horizontal.sqrMagnitude <= MinMoveSqrMagnitude || _maxStepHeight <= _skinWidth)
        {
            return false;
        }

        Vector3 start = transform.position;
        Vector3 direction = horizontal.normalized;
        float forwardDistance = Mathf.Max(horizontal.magnitude, _stepForwardOvershoot);
        Vector3 elevatedStart = start + Vector3.up * _maxStepHeight;

        if (CheckBody(elevatedStart))
        {
            return false;
        }

        if (CastBody(elevatedStart, direction, forwardDistance + _skinWidth, out RaycastHit clearanceHit)
            && !IsWalkableSurface(clearanceHit.normal))
        {
            return false;
        }

        Vector3 targetFoot = start + direction * forwardDistance;
        Vector3 landingProbeFoot = targetFoot + direction * Mathf.Max(
            0.01f,
            _capsuleRadius + _stepForwardOvershoot - _skinWidth);
        float stepProbeStart = _maxStepHeight + GetProbeRadius() + _skinWidth;
        float stepProbeDistance = _maxStepHeight + _groundSnapDistance + _skinWidth;

        if (!ProbeGroundAt(landingProbeFoot, stepProbeStart, stepProbeDistance, out PlayerGroundHit landingHit)
            || !landingHit.IsWalkable)
        {
            return false;
        }

        float heightDelta = landingHit.Point.y - start.y;
        if (heightDelta < _minStepHeight || heightDelta > _maxStepHeight + _skinWidth)
        {
            return false;
        }

        Vector3 targetPosition = new Vector3(targetFoot.x, landingHit.Point.y, targetFoot.z);
        if (CheckBody(targetPosition + Vector3.up * _skinWidth))
        {
            return false;
        }

        stepDelta = targetPosition - start;
        SteppedUp = true;
        LastStepHeight = heightDelta;
        return true;
    }

    private bool CheckBody(Vector3 position)
    {
        GetCapsulePoints(position, out Vector3 bottom, out Vector3 top);
        float radius = Mathf.Max(0.01f, _capsuleRadius - _skinWidth);

        return Physics.CheckCapsule(
            bottom,
            top,
            radius,
            _collisionLayers,
            QueryTriggerInteraction.Ignore);
    }

    public bool SnapToGround()
    {
        float distance = Mathf.Max(GetProbeDistance(), GetProbeStartHeight() + _groundSnapDistance + _skinWidth);

        if (!_groundProbe.TryProbeGround(transform.position+ Vector3.up * _groundProbe.ProbeStartHeight, distance, out PlayerGroundHit hit)
            || !hit.IsWalkable)
        {
            GroundHit = hit;
            return false;
        }

        float verticalDelta = CalculateGroundHeightAtPosition(hit.Point, hit.Normal, transform.position) - transform.position.y;
        if (verticalDelta >= _skinWidth)
        {
            Debug.Log("angle: "+_groundProbe.SlopeAngle);
            Debug.LogWarning("不应该向上吸附过大距离");
            return false;
            // Debug.Log("hit.Point          : " + hit.Point);
            // Debug.Log("verticalDelta      : " + verticalDelta);
            // Debug.Log("transform.position : " + transform.position);
            // Debug.Log("Angle              : " + Vector3.Angle(hit.Normal, Vector3.up));
        }
        // float verticalDelta = hit.Point.y - transform.position.y;
        if (verticalDelta > _maxStepHeight + _skinWidth || verticalDelta < -_groundSnapDistance - _skinWidth)
        {
            GroundHit = hit;
            return false;
        }

        if (Mathf.Abs(verticalDelta) > _skinWidth)
        {
            ApplyPositionDelta(Vector3.up * verticalDelta);
            SnappedToGround = true;
        }

        GroundHit = hit;
        return true;
    }

    private float CalculateGroundHeightAtPosition(Vector3 pointOnGround, Vector3 groundNormal, Vector3 position)
    {
        if (Mathf.Abs(groundNormal.y) <= 0.0001f)
        {
            return pointOnGround.y;
        }
        float xOffset = position.x - pointOnGround.x;
        float zOffset = position.z - pointOnGround.z;
        return pointOnGround.y - (groundNormal.x * xOffset + groundNormal.z * zOffset)/groundNormal.y;
    }

    private void RefreshGround()
    {
        if (_groundProbe != null)
        {
            _groundProbe.ProbeGround();
            GroundHit = _groundProbe.CurrentHit;
            return;
        }

        GroundHit = ProbeGroundAt(transform.position, GetProbeStartHeight(), GetProbeDistance(), out PlayerGroundHit hit)
            ? hit
            : PlayerGroundHit.None;
    }

    private bool ProbeGroundAt(Vector3 worldPosition, float startHeight, float distance, out PlayerGroundHit hit)
    {
        Vector3 origin = worldPosition + Vector3.up * Mathf.Max(0.0f, startHeight);
        bool hasHit = Physics.SphereCast(
            origin,
            GetProbeRadius(),
            Vector3.down,
            out RaycastHit raycastHit,
            distance,
            GetGroundLayers(),
            QueryTriggerInteraction.Ignore);

        hit = hasHit ? PlayerGroundHit.FromRaycast(raycastHit, GetMaxWalkableSlope()) : PlayerGroundHit.None;
        return hasHit;
    }

    private bool IsWalkableSurface(Vector3 normal)
    {
        if (normal.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        float minWalkableY = Mathf.Cos(GetMaxWalkableSlope() * Mathf.Deg2Rad);
        return normal.normalized.y >= minWalkableY;
    }

    private LayerMask GetGroundLayers()
    {
        return _groundProbe == null ? _collisionLayers : _groundProbe.GroundLayers;
    }

    private float GetProbeRadius()
    {
        return _groundProbe == null ? _groundProbeRadius : _groundProbe.SphereRadius;
    }

    private float GetProbeStartHeight()
    {
        return _groundProbe == null ? _groundProbeStartHeight : _groundProbe.ProbeStartHeight;
    }

    private float GetProbeDistance()
    {
        return _groundProbe == null ? _groundProbeDistance : _groundProbe.ProbeDistance;
    }

    private float GetMaxWalkableSlope()
    {
        return _groundProbe == null ? _maxWalkableSlope : _groundProbe.MaxWalkableSlope;
    }

    private void ApplyPositionDelta(Vector3 delta)
    {
        if (delta.y>0.01f)
        {
            Debug.Log("deltaY: "+delta.y);
            Debug.Log("angle: "+_groundProbe.SlopeAngle);
        }
        transform.position += delta;
        LastAppliedDisplacement += delta;
    }

    private void GetCapsulePoints(out Vector3 bottom, out Vector3 top)
    {
        GetCapsulePoints(transform.position, out bottom, out top);
    }

    private void GetCapsulePoints(Vector3 position, out Vector3 bottom, out Vector3 top)
    {
        float radius = Mathf.Max(0.01f, _capsuleRadius);
        float height = Mathf.Max(_capsuleHeight, radius * 2.0f);

        bottom = position + Vector3.up * radius;
        top = position + Vector3.up * (height - radius);
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos)
        {
            return;
        }

        GetCapsulePoints(out Vector3 bottom, out Vector3 top);
        Gizmos.color = HitSomething ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(bottom, _capsuleRadius);
        Gizmos.DrawWireSphere(top, _capsuleRadius);
        Gizmos.DrawLine(bottom + Vector3.forward * _capsuleRadius, top + Vector3.forward * _capsuleRadius);
        Gizmos.DrawLine(bottom - Vector3.forward * _capsuleRadius, top - Vector3.forward * _capsuleRadius);
        Gizmos.DrawLine(bottom + Vector3.right * _capsuleRadius, top + Vector3.right * _capsuleRadius);
        Gizmos.DrawLine(bottom - Vector3.right * _capsuleRadius, top - Vector3.right * _capsuleRadius);

        Gizmos.DrawWireSphere(hitPoint1, 0.05f);
    }
}
