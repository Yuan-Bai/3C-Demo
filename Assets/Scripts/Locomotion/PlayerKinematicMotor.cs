using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerKinematicMotor : MonoBehaviour
{
    [Header("Collision")]
    [SerializeField] private LayerMask _collisionLayers = ~0;
    [SerializeField, Min(0.2f)] private float _capsuleHeight = 1.6f;
    [SerializeField, Min(0.05f)] private float _capsuleRadius = 0.35f;
    [SerializeField, Min(0.0f)] private float _skinWidth = 0.03f;
    [SerializeField, Range(1, 5)] private int _maxSlideIterations = 3;

    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;

    public Vector3 LastRequestedDisplacement { get; private set; }
    public Vector3 LastAppliedDisplacement { get; private set; }
    public Vector3 LastHitNormal { get; private set; }
    public bool HitSomething { get; private set; }

    public Vector3 Move(Vector3 displacement)
    {
        LastRequestedDisplacement = displacement;
        LastAppliedDisplacement = Vector3.zero;
        LastHitNormal = Vector3.zero;
        HitSomething = false;

        Vector3 remaining = displacement;

        for (int i = 0; i < _maxSlideIterations; i++)
        {
            if (remaining.sqrMagnitude <= 0.000001f)
            {
                break;
            }

            Vector3 direction = remaining.normalized;
            float distance = remaining.magnitude;

            // CapsuleCast 是把整个角色胶囊沿某个方向“扫过去”，比一条 Ray 更接近真实身体体积。
            bool hasHit = CastBody(direction, distance + _skinWidth, out RaycastHit hit);

            if (!hasHit)
            {
                ApplyPositionDelta(remaining);
                break;
            }

            // 水平移动时可能扫到脚下地面。法线主要朝上的命中不是墙，先放行。
            // 坡面贴地和台阶抬升会在后续课程单独处理。
            if (hit.normal.y > 0.7f)
            {
                ApplyPositionDelta(remaining);
                break;
            }

            HitSomething = true;
            LastHitNormal = hit.normal;

            // skinWidth 是刻意保留的一点点缝隙，避免刚好贴住碰撞面后因为浮点误差卡进墙体。
            float safeDistance = Mathf.Max(0.0f, hit.distance - _skinWidth);
            Vector3 safeMove = direction * safeDistance;
            ApplyPositionDelta(safeMove);

            Vector3 leftover = direction * Mathf.Max(0.0f, distance - safeDistance);

            // ProjectOnPlane 会移除“撞进墙里”的分量，只保留沿墙表面的分量，所以角色能贴墙滑动。
            remaining = Vector3.ProjectOnPlane(leftover, hit.normal);
            remaining.y = 0.0f;
        }

        return LastAppliedDisplacement;
    }

    private bool CastBody(Vector3 direction, float distance, out RaycastHit hit)
    {
        GetCapsulePoints(out Vector3 bottom, out Vector3 top);
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

    private void ApplyPositionDelta(Vector3 delta)
    {
        transform.position += delta;
        LastAppliedDisplacement += delta;
    }

    private void GetCapsulePoints(out Vector3 bottom, out Vector3 top)
    {
        float radius = Mathf.Max(0.01f, _capsuleRadius);
        float height = Mathf.Max(_capsuleHeight, radius * 2.0f);

        // 这里约定 transform.position 是角色脚底中心点；胶囊底部球心要抬高一个半径。
        bottom = transform.position + Vector3.up * radius;
        top = transform.position + Vector3.up * (height - radius);
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
    }
}
