using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerGroundProbe : MonoBehaviour
{
    [Header("Probe")]
    [SerializeField] private LayerMask _groundLayers = ~0;
    [SerializeField, Min(0.01f)] private float _sphereRadius = 0.25f;
    [SerializeField, Min(0.0f)] private float _probeStartHeight = 0.6f;
    [SerializeField, Min(0.01f)] private float _probeDistance = 1.0f;
    [SerializeField, Range(0.0f, 89.0f)] private float _maxWalkableSlope = 45.0f;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;

    public bool IsGrounded { get; private set; }
    public bool IsWalkable { get; private set; }
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

    private Vector3 ProbeOrigin => transform.position + Vector3.up * _probeStartHeight;

    private void Update()
    {
        ProbeGround();
    }

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
            out RaycastHit raycastHit,
            distance,
            _groundLayers,
            QueryTriggerInteraction.Ignore);

        hit = hasHit ? PlayerGroundHit.FromRaycast(raycastHit, _maxWalkableSlope) : PlayerGroundHit.None;
        return hasHit;
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
