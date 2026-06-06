using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class PlayerKinematicMotorEditModeTests
{
    private readonly List<GameObject> _objects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = _objects.Count - 1; i >= 0; i--)
        {
            if (_objects[i] != null)
            {
                Object.DestroyImmediate(_objects[i]);
            }
        }

        _objects.Clear();
        Physics.SyncTransforms();
    }

    [Test]
    public void Move_FollowsWalkableSlopeUpAndDown()
    {
        const float angle = 20.0f;
        const float zMin = -3.0f;
        const float length = 6.0f;

        CreateRamp("Walkable Ramp", angle, zMin, length);

        float startZ = -2.5f;
        float startY = RampHeightAt(startZ, angle, zMin);
        PlayerKinematicMotor motor = CreateMotor(new Vector3(0.0f, startY, startZ));

        MoveRepeatedly(motor, Vector3.forward * 0.12f, 35);
        float uphillY = motor.transform.position.y;

        Assert.That(uphillY, Is.GreaterThan(startY + 0.75f));
        Assert.That(motor.IsOnWalkableGround, Is.True);
        Assert.That(motor.GroundSlopeAngle, Is.InRange(angle - 1.5f, angle + 1.5f));

        MoveRepeatedly(motor, Vector3.back * 0.12f, 35);

        Assert.That(motor.transform.position.y, Is.LessThan(uphillY - 0.75f));
        Assert.That(motor.transform.position.y, Is.EqualTo(startY).Within(0.12f));
        Assert.That(motor.IsOnWalkableGround, Is.True);
    }

    [Test]
    public void Move_StepsUpOntoLowObstacle()
    {
        const float stepHeight = 0.25f;

        CreateBox("Ground", new Vector3(0.0f, -0.05f, 0.0f), new Vector3(4.0f, 0.1f, 2.2f));
        CreateBox("Step", new Vector3(0.0f, stepHeight * 0.5f, 1.75f), new Vector3(4.0f, stepHeight, 1.5f));

        PlayerKinematicMotor motor = CreateMotor(new Vector3(0.0f, 0.0f, 0.2f));
        MoveSummary summary = MoveRepeatedly(motor, Vector3.forward * 0.1f, 22);

        Assert.That(summary.SteppedUp, Is.True);
        Assert.That(motor.transform.position.y, Is.EqualTo(stepHeight).Within(0.08f));
        Assert.That(summary.MaxStepHeight, Is.InRange(stepHeight - 0.05f, stepHeight + 0.05f));
        Assert.That(motor.IsOnWalkableGround, Is.True);
    }

    [Test]
    public void Move_DoesNotStepOntoTallWall()
    {
        const float wallHeight = 0.7f;

        CreateBox("Ground", new Vector3(0.0f, -0.05f, 0.0f), new Vector3(4.0f, 0.1f, 2.2f));
        CreateBox("Wall", new Vector3(0.0f, wallHeight * 0.5f, 1.75f), new Vector3(4.0f, wallHeight, 1.5f));

        PlayerKinematicMotor motor = CreateMotor(new Vector3(0.0f, 0.0f, 0.2f));
        MoveSummary summary = MoveRepeatedly(motor, Vector3.forward * 0.1f, 22);

        Assert.That(summary.SteppedUp, Is.False);
        Assert.That(motor.transform.position.y, Is.EqualTo(0.0f).Within(0.05f));
        Assert.That(motor.transform.position.z, Is.LessThan(0.75f));
    }

    [Test]
    public void Move_SnapsDownSmallStep()
    {
        const float platformHeight = 0.25f;

        CreateBox("High Platform", new Vector3(0.0f, platformHeight * 0.5f, -0.1f), new Vector3(4.0f, platformHeight, 1.8f));
        CreateBox("Lower Ground", new Vector3(0.0f, -0.05f, 2.2f), new Vector3(4.0f, 0.1f, 2.8f));

        PlayerKinematicMotor motor = CreateMotor(new Vector3(0.0f, platformHeight, 0.3f));
        bool snapped = false;

        for (int i = 0; i < 24; i++)
        {
            motor.Move(Vector3.forward * 0.1f);
            snapped |= motor.SnappedToGround;
            Physics.SyncTransforms();
        }

        Assert.That(snapped, Is.True);
        Assert.That(motor.transform.position.y, Is.EqualTo(0.0f).Within(0.08f));
        Assert.That(motor.IsOnWalkableGround, Is.True);
    }

    [Test]
    public void TryGetGroundPose_AlignsPoseUpToGroundNormal()
    {
        const float angle = 20.0f;
        const float zMin = -3.0f;
        const float length = 6.0f;

        CreateRamp("IK Ramp", angle, zMin, length);
        PlayerKinematicMotor motor = CreateMotor(new Vector3(0.0f, RampHeightAt(0.0f, angle, zMin), 0.0f));

        bool hasPose = motor.TryGetGroundPose(motor.transform.position, Vector3.forward, out Pose pose);

        Vector3 surfaceUp = pose.rotation * Vector3.up;
        Vector3 expectedNormal = Quaternion.AngleAxis(-angle, Vector3.right) * Vector3.up;

        Assert.That(hasPose, Is.True);
        Assert.That(Vector3.Angle(surfaceUp, expectedNormal), Is.LessThan(2.0f));
        Assert.That(pose.position.y, Is.EqualTo(RampHeightAt(0.0f, angle, zMin)).Within(0.08f));
        Assert.That(motor.MaxWalkableSlope, Is.EqualTo(45.0f).Within(0.01f));
        Assert.That(motor.GroundProbeStartHeight, Is.EqualTo(0.6f).Within(0.01f));
        Assert.That(motor.GroundProbeDistance, Is.EqualTo(1.0f).Within(0.01f));
    }

    private PlayerKinematicMotor CreateMotor(Vector3 footPosition)
    {
        GameObject go = new GameObject("Test Motor");
        _objects.Add(go);
        go.transform.position = footPosition;
        PlayerKinematicMotor motor = go.AddComponent<PlayerKinematicMotor>();
        Physics.SyncTransforms();
        return motor;
    }

    private MoveSummary MoveRepeatedly(PlayerKinematicMotor motor, Vector3 displacement, int count)
    {
        bool steppedUp = false;
        float maxStepHeight = 0.0f;

        for (int i = 0; i < count; i++)
        {
            motor.Move(displacement);
            steppedUp |= motor.SteppedUp;
            maxStepHeight = Mathf.Max(maxStepHeight, motor.LastStepHeight);
            Physics.SyncTransforms();
        }

        return new MoveSummary(steppedUp, maxStepHeight);
    }

    private GameObject CreateBox(string name, Vector3 center, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _objects.Add(go);
        go.name = name;
        go.transform.position = center;
        go.transform.localScale = scale;
        Physics.SyncTransforms();
        return go;
    }

    private GameObject CreateRamp(string name, float angle, float zMin, float length)
    {
        const float width = 4.0f;
        const float thickness = 0.2f;

        float height = Mathf.Tan(angle * Mathf.Deg2Rad) * length;
        float zMax = zMin + length;

        Mesh mesh = new Mesh
        {
            name = name + " Mesh",
            vertices = new[]
            {
                new Vector3(-width * 0.5f, -thickness, zMin),
                new Vector3(width * 0.5f, -thickness, zMin),
                new Vector3(-width * 0.5f, -thickness, zMax),
                new Vector3(width * 0.5f, -thickness, zMax),
                new Vector3(-width * 0.5f, 0.0f, zMin),
                new Vector3(width * 0.5f, 0.0f, zMin),
                new Vector3(-width * 0.5f, height, zMax),
                new Vector3(width * 0.5f, height, zMax),
            },
            triangles = new[]
            {
                4, 6, 5, 5, 6, 7,
                0, 1, 2, 1, 3, 2,
                0, 4, 1, 1, 4, 5,
                2, 3, 6, 3, 7, 6,
                0, 2, 4, 2, 6, 4,
                1, 5, 3, 3, 5, 7,
            },
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject go = new GameObject(name);
        _objects.Add(go);
        MeshCollider collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        Physics.SyncTransforms();
        return go;
    }

    private static float RampHeightAt(float z, float angle, float zMin)
    {
        return Mathf.Tan(angle * Mathf.Deg2Rad) * (z - zMin);
    }

    private readonly struct MoveSummary
    {
        public MoveSummary(bool steppedUp, float maxStepHeight)
        {
            SteppedUp = steppedUp;
            MaxStepHeight = maxStepHeight;
        }

        public bool SteppedUp { get; }
        public float MaxStepHeight { get; }
    }
}
