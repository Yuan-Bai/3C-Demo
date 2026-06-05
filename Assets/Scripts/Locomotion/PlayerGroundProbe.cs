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
    [field: SerializeField]public float SlopeAngle { get; private set; }

    private Vector3 ProbeOrigin => transform.position + Vector3.up * _probeStartHeight;

    private void Update()
    {
        ProbeGround();
    }

    public void ProbeGround()
    {
        Vector3 origin = ProbeOrigin;
        // 进行球形探测，防止漏判
        bool hasHit = Physics.SphereCast(
            origin, 
            _sphereRadius, 
            Vector3.down, 
            out RaycastHit hit, 
            _probeDistance, 
            _groundLayers, 
            QueryTriggerInteraction.Ignore);
        if (hasHit)
        {
            ClearGround();
            return;
        }

        IsGrounded = true;
        GroundPoint = hit.point;
        GroundNormal = hit.normal;
        GroundDistance = hit.distance;

        // 坡度角，相对于世界xz平面的夹角
        SlopeAngle = Vector3.Angle(GroundNormal, Vector3.up);

        IsWalkable = SlopeAngle <= _maxWalkableSlope;
    }

    private void ClearGround()
    {
        IsGrounded = false;
        IsWalkable = false;
        GroundPoint = Vector3.zero;
        GroundNormal = Vector3.up;
        GroundDistance = 0.0f;
        SlopeAngle = 0.0f;
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
