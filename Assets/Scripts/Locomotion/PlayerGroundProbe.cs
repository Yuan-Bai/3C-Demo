using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerGroundProbe : MonoBehaviour
{
    private const float MinGroundUpTolerance = 0.005f;
    private const float MaxGroundUpTolerance = 0.02f;
    private const float MinSupportHeightTolerance = 0.02f;
    private const float MaxSupportNormalAngle = 15.0f;
    private const int MinSideSupportCount = 2;

    private readonly RaycastHit[] _supportHits = new RaycastHit[8];

    [Header("Probe")]
    [SerializeField] private LayerMask _groundLayers = ~0;
    [SerializeField, Min(0.01f)] private float _sphereRadius = 0.25f;
    [SerializeField, Min(0.0f)] private float _probeStartHeight = 0.6f;
    [SerializeField, Min(0.01f)] private float _probeDistance = 1.0f;
    [SerializeField, Range(0.0f, 89.0f)] private float _maxWalkableSlope = 45.0f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;

    #region 供外部访问属性
    [field: SerializeField] public bool IsGrounded { get; private set; }
    [field: SerializeField] public bool IsWalkable { get; private set; }
    public Vector3 GroundPoint { get; private set; }
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public float GroundDistance { get; private set; }
    [field: SerializeField] public float SlopeAngle { get; private set; }
    public PlayerGroundHit CurrentHit { get; private set; }

    public LayerMask GroundLayers => _groundLayers;
    public float SphereRadius => _sphereRadius;
    public float ProbeStartHeight => _probeStartHeight;
    public float ProbeDistance => _probeDistance;
    public float MaxWalkableSlope => _maxWalkableSlope;
    #endregion

    private Vector3 ProbeOrigin => transform.position + Vector3.up * _probeStartHeight;

    public void ProbeGround()
    {
        if (!TryProbeGround(out PlayerGroundHit hit))
        {
            ClearGround();
            return;
        }

        IsGrounded = true;
        CurrentHit = hit;
        GroundPoint = hit.Point;
        GroundNormal = hit.Normal;
        GroundDistance = hit.Distance;

        SlopeAngle = hit.SlopeAngle;

        IsWalkable = hit.IsWalkable;
    }

    public bool TryProbeGround(out PlayerGroundHit hit)
    {
        return TryProbeGround(ProbeOrigin, _sphereRadius, _probeDistance, out hit);
    }

    public bool TryProbeGround(Vector3 origin, float distance, out PlayerGroundHit hit)
    {
        return TryProbeGround(origin, _sphereRadius, distance, out hit);
    }

    public bool TryProbeGround(Vector3 origin, float radius, float distance, out PlayerGroundHit hit)
    {
        bool hasHit = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out RaycastHit mainHit,
            distance,
            _groundLayers,
            QueryTriggerInteraction.Ignore);

        if (!hasHit)
        {
            hit = PlayerGroundHit.None;
            return false;
        }

        float footY = origin.y - _probeStartHeight;
        float rayDistance = Mathf.Max(distance + radius, _probeStartHeight);
        float maxDownDistance = Mathf.Max(0.0f, rayDistance - _probeStartHeight);
        float upTolerance = GetGroundUpTolerance(radius);

        // The wide sphere cast is only a broad-phase candidate. A center ray is
        // the most reliable proof that the surface is actually under the body.
        if (TryGetValidRayGround(origin, rayDistance, footY, maxDownDistance, upTolerance, out RaycastHit centerHit))
        {
            hit = PlayerGroundHit.FromRaycast(centerHit, _maxWalkableSlope);
            return true;
        }

        int supportCount = CollectSideSupportHits(origin, radius, rayDistance, footY, maxDownDistance, upTolerance);
        if (TrySelectSupportedHit(supportCount, radius, footY, out RaycastHit supportHit))
        {
            hit = PlayerGroundHit.FromRaycast(supportHit, _maxWalkableSlope);
            return true;
        }

        // Reject edge-only sphere hits. They can have a walkable interpolated
        // normal, but they are not proven support under the character.
        hit = PlayerGroundHit.FromRaycast(mainHit, _maxWalkableSlope);
        return false;
    }

    public bool TryProbeGroundAt(Vector3 worldPosition, float startHeight, float distance, out PlayerGroundHit hit)
    {
        Vector3 origin = worldPosition + Vector3.up * Mathf.Max(0.0f, startHeight);
        return TryProbeGround(origin, _sphereRadius, distance, out hit);
    }

    public bool TryProbeGroundAt(Vector3 worldPosition, out PlayerGroundHit hit)
    {
        return TryProbeGroundAt(worldPosition, _probeStartHeight, _probeDistance, out hit);
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
        return TryGetGroundPose(worldPosition, forwardHint, _probeStartHeight, _probeDistance, out pose);
    }

    private void ClearGround()
    {
        IsGrounded = false;
        IsWalkable = false;
        GroundPoint = Vector3.zero;
        GroundNormal = Vector3.up;
        GroundDistance = 0.0f;
        SlopeAngle = 0.0f;
        CurrentHit = PlayerGroundHit.None;
    }

    private int CollectSideSupportHits(
        Vector3 origin,
        float radius,
        float rayDistance,
        float footY,
        float maxDownDistance,
        float upTolerance)
    {
        int count = 0;
        float sampleRadius = Mathf.Max(0.01f, radius * 0.75f);
        Vector3 forward = ProjectHorizontal(transform.forward, Vector3.forward);
        Vector3 right = ProjectHorizontal(transform.right, Vector3.right);

        TryAddSupportHit(origin + forward * sampleRadius, rayDistance, footY, maxDownDistance, upTolerance, ref count);
        TryAddSupportHit(origin - forward * sampleRadius, rayDistance, footY, maxDownDistance, upTolerance, ref count);
        TryAddSupportHit(origin + right * sampleRadius, rayDistance, footY, maxDownDistance, upTolerance, ref count);
        TryAddSupportHit(origin - right * sampleRadius, rayDistance, footY, maxDownDistance, upTolerance, ref count);

        float diagonalRadius = sampleRadius * 0.70710678f;
        TryAddSupportHit(origin + (forward + right) * diagonalRadius, rayDistance, footY, maxDownDistance, upTolerance, ref count);
        TryAddSupportHit(origin + (forward - right) * diagonalRadius, rayDistance, footY, maxDownDistance, upTolerance, ref count);
        TryAddSupportHit(origin + (-forward + right) * diagonalRadius, rayDistance, footY, maxDownDistance, upTolerance, ref count);
        TryAddSupportHit(origin - (forward + right) * diagonalRadius, rayDistance, footY, maxDownDistance, upTolerance, ref count);

        return count;
    }

    private void TryAddSupportHit(
        Vector3 origin,
        float rayDistance,
        float footY,
        float maxDownDistance,
        float upTolerance,
        ref int count)
    {
        if (count >= _supportHits.Length)
        {
            return;
        }

        if (TryGetValidRayGround(origin, rayDistance, footY, maxDownDistance, upTolerance, out RaycastHit hit))
        {
            _supportHits[count] = hit;
            count++;
        }
    }

    private bool TryGetValidRayGround(
        Vector3 origin,
        float rayDistance,
        float footY,
        float maxDownDistance,
        float upTolerance,
        out RaycastHit hit)
    {
        bool hasHit = Physics.Raycast(
            origin,
            Vector3.down,
            out hit,
            rayDistance,
            _groundLayers,
            QueryTriggerInteraction.Ignore);

        return hasHit && IsValidGroundHit(hit, footY, maxDownDistance, upTolerance);
    }

    private bool IsValidGroundHit(RaycastHit hit, float footY, float maxDownDistance, float upTolerance)
    {
        if (!IsWalkableNormal(hit.normal))
        {
            return false;
        }

        float heightDelta = hit.point.y - footY;
        return heightDelta <= upTolerance && heightDelta >= -maxDownDistance;
    }

    private bool TrySelectSupportedHit(int count, float radius, float footY, out RaycastHit hit)
    {
        hit = default;
        if (count < MinSideSupportCount)
        {
            return false;
        }

        float heightTolerance = Mathf.Max(MinSupportHeightTolerance, radius * 0.25f);
        float minNormalDot = Mathf.Cos(MaxSupportNormalAngle * Mathf.Deg2Rad);
        int bestSupportCount = 0;
        float bestHeightError = float.PositiveInfinity;
        int bestIndex = -1;

        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = _supportHits[i];
            int candidateSupportCount = 0;

            for (int j = 0; j < count; j++)
            {
                RaycastHit other = _supportHits[j];
                if (Mathf.Abs(other.point.y - candidate.point.y) > heightTolerance)
                {
                    continue;
                }

                if (Vector3.Dot(other.normal.normalized, candidate.normal.normalized) < minNormalDot)
                {
                    continue;
                }

                candidateSupportCount++;
            }

            float heightError = Mathf.Abs(candidate.point.y - footY);
            if (candidateSupportCount > bestSupportCount
                || candidateSupportCount == bestSupportCount && heightError < bestHeightError)
            {
                bestSupportCount = candidateSupportCount;
                bestHeightError = heightError;
                bestIndex = i;
            }
        }

        if (bestIndex < 0 || bestSupportCount < MinSideSupportCount)
        {
            return false;
        }

        hit = _supportHits[bestIndex];
        return true;
    }

    private bool IsWalkableNormal(Vector3 normal)
    {
        if (normal.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        float minWalkableY = Mathf.Cos(_maxWalkableSlope * Mathf.Deg2Rad);
        return normal.normalized.y >= minWalkableY;
    }

    private static float GetGroundUpTolerance(float radius)
    {
        return Mathf.Clamp(radius * 0.1f, MinGroundUpTolerance, MaxGroundUpTolerance);
    }

    private static Vector3 ProjectHorizontal(Vector3 direction, Vector3 fallback)
    {
        Vector3 projected = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (projected.sqrMagnitude <= 0.0001f)
        {
            projected = Vector3.ProjectOnPlane(fallback, Vector3.up);
        }

        return projected.sqrMagnitude <= 0.0001f ? Vector3.forward : projected.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos)
        {
            return;
        }

        Vector3 origin = ProbeOrigin;
        Vector3 end = origin + Vector3.down * _probeDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, _sphereRadius);
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, _sphereRadius);

        if (!Application.isPlaying || !IsGrounded)
        {
            return;
        }

        Gizmos.color = IsWalkable ? Color.green : Color.red;
        Gizmos.DrawSphere(GroundPoint, 0.05f);
        Gizmos.DrawRay(GroundPoint, GroundNormal * 0.5f);
    }
}
