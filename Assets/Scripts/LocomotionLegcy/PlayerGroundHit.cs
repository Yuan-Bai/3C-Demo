using UnityEngine;

namespace ThreeCDemo.Locomotion.CustomKcc
{
    public readonly struct PlayerGroundHit
    {
        public static PlayerGroundHit None => default;

        public bool HasHit { get; }
        public bool IsWalkable { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Distance { get; }
        public float SlopeAngle { get; }
        public Collider Collider { get; }
        public Rigidbody Rigidbody { get; }
        public PhysicMaterial PhysicMaterial { get; }
        public RaycastHit RaycastHit { get; }

        private PlayerGroundHit(
            bool hasHit,
            bool isWalkable,
            Vector3 point,
            Vector3 normal,
            float distance,
            float slopeAngle,
            Collider collider,
            Rigidbody rigidbody,
            PhysicMaterial physicMaterial,
            RaycastHit raycastHit)
        {
            HasHit = hasHit;
            IsWalkable = isWalkable;
            Point = point;
            Normal = normal;
            Distance = distance;
            SlopeAngle = slopeAngle;
            Collider = collider;
            Rigidbody = rigidbody;
            PhysicMaterial = physicMaterial;
            RaycastHit = raycastHit;
        }

        public static PlayerGroundHit FromRaycast(RaycastHit hit, float maxWalkableSlope)
        {
            Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
            float slopeAngle = Vector3.Angle(normal, Vector3.up);
            Collider collider = hit.collider;

            return new PlayerGroundHit(
                true,
                CalculateIsWalkable(slopeAngle, maxWalkableSlope),
                hit.point,
                normal,
                hit.distance,
                slopeAngle,
                collider,
                collider == null ? null : collider.attachedRigidbody,
                collider == null ? null : collider.sharedMaterial,
                hit);
        }

        /// <summary>
        /// 计算是否可走，使用函数方便以后添加规则
        /// </summary>
        /// <param name="slopeAngle"></param>
        /// <param name="maxWalkableSlope"></param>
        /// <returns></returns>
        public static bool CalculateIsWalkable(float slopeAngle, float maxWalkableSlope)
        {
            return slopeAngle <= maxWalkableSlope;
        }

        /// <summary>
        /// 无法获取问题所处平面法线时，默认放在水平地面上
        /// </summary>
        /// <param name="position"></param>
        /// <param name="forwardHint"></param>
        /// <returns>返回Pose(pos, quaternion)</returns>
        public static Pose CreateFallbackPose(Vector3 position, Vector3 forwardHint)
        {
            return new Pose(position, CreateSurfaceRotation(Vector3.up, forwardHint));
        }

        /// <summary>
        /// 将物体的forward和投影到平面的forward尽可能一致，物体的up和平面的normal尽可能一致
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="forwardHint"></param>
        /// <returns></returns>
        public static Quaternion CreateSurfaceRotation(Vector3 normal, Vector3 forwardHint)
        {
            normal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(forwardHint, normal);

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.forward, normal);
            }

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.right, normal);
            }

            return Quaternion.LookRotation(forward.normalized, normal);
        }

        /// <summary>
        /// 获取hitpoint这一点，forward投影在hitnormal对应平面的位置和四元数
        /// </summary>
        /// <param name="forwardHint"></param>
        /// <returns></returns>
        public Pose GetSurfacePose(Vector3 forwardHint)
        {
            return new Pose(Point, GetSurfaceRotation(forwardHint));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forwardHint"></param>
        /// <returns></returns>
        public Quaternion GetSurfaceRotation(Vector3 forwardHint)
        {
            Vector3 normal = HasHit && Normal.sqrMagnitude > 0.0001f ? Normal.normalized : Vector3.up;
            return CreateSurfaceRotation(normal, forwardHint);
        }
    }
}