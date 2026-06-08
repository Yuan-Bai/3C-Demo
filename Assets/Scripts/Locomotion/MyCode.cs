// using Unity.VisualScripting;
// using UnityEngine;

// namespace ThreeCDemo.Locomotion.CustomKcc
// {
//     [DisallowMultipleComponent]
//     public sealed class MyCode : MonoBehaviour
//     {
//         private const float MinMoveSqrMagnitude = 0.000001f;
//         private const int MaxDepenetrationIterations = 4;
//         private const int MaxOverlapResults = 16;
//         // 重叠分离后保持的padding
//         private const float DepenetrationPadding = 0.001f;
//         // 设置为只读还可以加快速度
//         private readonly Collider[] _overlapResults = new Collider[MaxOverlapResults];
//         private CapsuleCollider _depenetrationCapsule;

//         [Header("References")]
//         [SerializeField] private PlayerGroundProbe _groundProbe;

//         [Header("Collision")]
//         [SerializeField] private LayerMask _collisionLayers = ~0;
//         [SerializeField, Min(0.2f)] private float _capsuleHeight = 1.52f;
//         [SerializeField, Min(0.05f)] private float _capsuleRadius = 0.15f;
//         [SerializeField] private Vector3 _capsuleOffset = Vector3.zero;
//         [SerializeField] private Vector3 _capsuleBottomCenter => transform.position + _capsuleOffset;
//         [SerializeField, Min(0.0001f)] private float _skinWidth = 0.01f;
//         [SerializeField, Range(1, 5)] private int _maxSlideIterations = 3;

//         [Header("Steps")]
//         [SerializeField, Min(0.0f)] private float _maxStepHeight = 0.35f;
//         [SerializeField, Min(0.0f)] private float _minStepHeight = 0.03f;
//         [SerializeField, Min(0.0f)] private float _stepForwardOvershoot = 0.06f;

//         [Header("Debug")]
//         [SerializeField] private bool _drawGizmos = true;

//         #region 供外部访问属性
//         public Vector3 LastRequestedDisplacement { get; private set; }
//         public Vector3 LastAppliedDisplacement { get; private set; }
//         public Vector3 LastHitNormal { get; private set; }
//         public bool HitSomething { get; private set; }
//         public PlayerGroundHit GroundHit { get; private set; }
//         public bool IsGrounded => GroundHit.HasHit;
//         public bool IsOnWalkableGround => GroundHit.HasHit && GroundHit.IsWalkable;
//         public Vector3 GroundPoint => GroundHit.Point;
//         public Vector3 GroundNormal => IsGrounded ? GroundHit.Normal : Vector3.up;
//         public float GroundSlopeAngle => GroundHit.SlopeAngle;
//         public Vector3 LastVelocity { get; private set; }
//         public Vector3 LastHorizontalVelocity { get; private set; }
//         public bool SteppedUp { get; private set; }
//         public bool SnappedToGround { get; private set; }
//         public float LastStepHeight { get; private set; }
//         public LayerMask CollisionLayers => _collisionLayers;
//         #endregion

//         private void Awake()
//         {
//             _groundProbe??=GetComponent<PlayerGroundProbe>();
//         }

//         private void OnDestroy()
//         {
//             if (_depenetrationCapsule != null)
//             {
//                 Destroy(_depenetrationCapsule.gameObject);
//                 _depenetrationCapsule = null;
//             }
//         }

//         public Vector3 Move(Vector3 displacement)
//         {
//             LastRequestedDisplacement = displacement;
//             LastAppliedDisplacement = Vector3.zero;
//             LastHitNormal = Vector3.zero;
//             HitSomething = false;
//             SteppedUp = false;
//             SnappedToGround = false;
//             LastStepHeight = 0.0f;

//             Vector3 adjustedDisplacement = AdjustForGround(displacement);
//             MoveWithCollision(adjustedDisplacement);

//             return Vector3.zero;
//         }

//         private Vector3 AdjustForGround(Vector3 displacement)
//         {
//             if (!IsOnWalkableGround)
//             {
//                 return displacement;
//             }

//             // 去除包含跳跃、重力、下落等 y 分量，用于计算slopeDirection
//             Vector3 horizontal = Vector3.ProjectOnPlane(displacement, Vector3.up);
//             if (horizontal.sqrMagnitude <= MinMoveSqrMagnitude)
//             {
//                 return displacement;
//             }

//             Vector3 slopeDirection = Vector3.ProjectOnPlane(horizontal, GroundNormal);
//             if (slopeDirection.sqrMagnitude <= MinMoveSqrMagnitude)
//             {
//                 return displacement;
//             }

//             Vector3 vertical = Vector3.up * displacement.y;
//             return slopeDirection.normalized * horizontal.magnitude + vertical;
//         }

//         private void MoveWithCollision(Vector3 displacement)
//         {
//             // CapsuleCast 不会检测与起始胶囊已经重叠的碰撞体，可能会导致穿墙
//             ResolveBodyOverlaps();

//             Vector3 remaining = displacement;

//             for (int i = 0; i < _maxSlideIterations; i++)
//             {
//                 if (remaining.sqrMagnitude <= MinMoveSqrMagnitude)
//                 {
//                     break;
//                 }

//                 Vector3 direction = remaining.normalized;
//                 float distance = remaining.magnitude;

//                 bool hasHit = CastBody(direction, distance + _skinWidth, out RaycastHit hit);
//                 if (!hasHit)
//                 {
//                     ApplyPositionDelta(remaining);
//                     ResolveBodyOverlaps();
//                     break;
//                 }
//             }
//         }

//         private void ApplyPositionDelta(Vector3 delta)
//         {
//             transform.position += delta;
//             LastAppliedDisplacement += delta;
//         }

//         private bool CastBody(Vector3 direction, float distance, out RaycastHit hit)
//         {
//             return CastBody(_capsuleBottomCenter, direction, distance, out hit);
//         }

//         private bool CastBody(Vector3 position, Vector3 direction, float distance, out RaycastHit hit)
//         {
//             GetCapsulePoints(position, out Vector3 bottom, out Vector3 top);
//             return Physics.CapsuleCast(
//                 bottom,
//                 top,
//                 _capsuleRadius,
//                 direction,
//                 out hit,
//                 distance,
//                 _collisionLayers,
//                 QueryTriggerInteraction.Ignore);
//         }

//         private bool ResolveBodyOverlaps()
//         {
//             bool moved = false;

//             for (int i = 0; i < MaxDepenetrationIterations; i++)
//             {
//                 if (!TryResolveSingleBodyOverlap())
//                 {
//                     break;
//                 }

//                 moved = true;
//             }

//             return moved;
//         }

//         private bool TryResolveSingleBodyOverlap()
//         {
//             GetCapsulePoints(out Vector3 bottom, out Vector3 top);
//             int count = Physics.OverlapCapsuleNonAlloc(
//                 bottom,
//                 top,
//                 _capsuleRadius,
//                 _overlapResults,
//                 _collisionLayers,
//                 QueryTriggerInteraction.Ignore
//             );

//             if (count <= 0)
//             {
//                 return false;
//             }

//             CapsuleCollider bodyCollider = GetDepenetrationCapusle();
//             ConfigureDepenetrationCapsule(bodyCollider, transform.position);
            
//             for (int i=0;i<count;i++)
//             {
//                 Collider other = _overlapResults[i];
//                 // 放在下次碰撞是产生污染
//                 _overlapResults[i] = null;

//                 if (ShouldIgnoreOverlapCollider(other))
//                 {
//                     continue;
//                 }

//                 bodyCollider.enabled = true;
//                 bool hasPenetration = Physics.ComputePenetration(
//                     bodyCollider,
//                     transform.position,
//                     Quaternion.identity,
//                     other,
//                     other.transform.position,
//                     other.transform.rotation,
//                     out Vector3 direction,
//                     out float distance);
//                 bodyCollider.enabled = false;

//                 // 如果没有重叠以及距离小于等于0就下一个
//                 if (!hasPenetration || distance <= 0.0f)
//                 {
//                     continue;
//                 }

//                 Vector3 correction = direction.normalized * (distance + DepenetrationPadding);
//                 correction = ConstrainDepenetrationCorrection(other, correction);
//                 if (correction.sqrMagnitude <= MinMoveSqrMagnitude)
//                 {
//                     continue;
//                 }

//                 ApplyPositionDelta(correction);
//                 return true;
//             }

//             bodyCollider.enabled = false;
//             return false;
//         }

//         private void GetCapsulePoints(out Vector3 bottom, out Vector3 top)
//         {
//             GetCapsulePoints(_capsuleBottomCenter, out bottom, out top);
//         }

//         private void GetCapsulePoints(Vector3 position, out Vector3 bottom, out Vector3 top)
//         {
//             float radius = Mathf.Max(0.01f, _capsuleRadius);
//             float height = Mathf.Max(_capsuleHeight, radius * 2.0f);

//             bottom = position + Vector3.up * radius;
//             top = position + Vector3.up * (height - radius);
//         }

//         private CapsuleCollider GetDepenetrationCapusle()
//         {
//             if (_depenetrationCapsule!=null)
//             {
//                 return _depenetrationCapsule;
//             }

//             GameObject helper = new GameObject("KCC DepenetrationCapusle")
//             {
//                 hideFlags = HideFlags.HideAndDontSave
//             };

//             _depenetrationCapsule = helper.AddComponent<CapsuleCollider>();
//             _depenetrationCapsule.direction = 1;
//             _depenetrationCapsule.enabled = false;
//             return _depenetrationCapsule;
//         }

//         private void ConfigureDepenetrationCapsule(CapsuleCollider capsule, Vector3 position)
//         {
//             float radius = Mathf.Max(0.01f, _capsuleRadius);
//             float height = Mathf.Max(2.0f*radius, _capsuleHeight);

//             capsule.radius = radius;
//             capsule.height = height;
//             capsule.center = Vector3.up * height * 0.5f + _capsuleOffset;
//             capsule.transform.SetPositionAndRotation(position, Quaternion.identity);
//         }

//         private bool ShouldIgnoreOverlapCollider(Collider other)
//         {
//             if (other == null || other == _depenetrationCapsule)
//             {
//                 return true;
//             }

//             Transform otherTransform = other.transform;
//             return otherTransform == transform || otherTransform.IsChildOf(transform);
//         }

//         /// <summary>
//         /// 如果y轴是的位移很小，在皮肤宽度误差范围以内
//         /// 或者碰撞体本身高度于脚底的差很小，运行y轴位移，其他情况不允许
//         /// </summary>
//         /// <param name="other"></param>
//         /// <param name="correction"></param>
//         /// <returns></returns>
//         private Vector3 ConstrainDepenetrationCorrection(Collider other, Vector3 correction)
//         {
//             if (correction.y <= _skinWidth)
//             {
//                 return correction;
//             }

//             float footY = transform.position.y;
//             if (other.bounds.max.y <= footY + _skinWidth * 2.0f)
//             {
//                 return correction;
//             }

//             Vector3 horizontal = Vector3.ProjectOnPlane(correction, Vector3.up);
//             if (horizontal.sqrMagnitude > MinMoveSqrMagnitude)
//             {
//                 return horizontal;
//             }

//             Vector3 away = transform.position - other.bounds.center;
//             away.y = 0.0f;
//             return away.sqrMagnitude > MinMoveSqrMagnitude
//                 ? away.normalized * correction.magnitude
//                 : Vector3.zero;
//         }
//     }
// }