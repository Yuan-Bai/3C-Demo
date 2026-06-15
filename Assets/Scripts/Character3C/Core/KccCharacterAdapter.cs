

using KinematicCharacterController;
using UnityEngine;

public sealed class KccCharacterAdapter : MonoBehaviour, ICharacterController, IKccMotorPort
{
    [SerializeField] private KinematicCharacterMotor _motor;
    [SerializeField] private CharacterCoordinator _coordinator;

    public bool IsGrounded => _motor.GroundingStatus.IsStableOnGround;

    public Vector3 GroundNormal => _motor.GroundingStatus.GroundNormal;

    public Vector3 CharacterUp => _motor.CharacterUp;

    public Vector3 Velocity => _motor.Velocity;

    private void Awake()
    {
        _motor ??= GetComponent<KinematicCharacterMotor>();
        _coordinator ??= GetComponent<CharacterCoordinator>();
        _motor.CharacterController = this;
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        // Debug.Log("BeforeCharacterUpdate");

        _coordinator?.BeforeCharacterUpdate(deltaTime);
    }

    public void PostGroundingUpdate(float deltaTime)
    {
        // Debug.Log("PostGroundingUpdate");
        _coordinator?.CurrentState.PostGroundingUpdate(deltaTime);
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // Debug.Log("UpdateRotation");
        _coordinator?.CurrentState.UpdateRotation(ref currentRotation, deltaTime);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // Debug.Log("UpdateVelocity");
        _coordinator?.CurrentState.UpdateVelocity(ref currentVelocity, deltaTime);
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        // Debug.Log("AfterCharacterUpdate");
        _coordinator?.AfterCharacterUpdate(deltaTime);
    }
    
    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void OnDiscreteCollisionDetected(Collider hitCollider)
    {
    }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
    {
    }

    public void ForceUnground(float duration) => _motor.ForceUnground(duration);
    public void SetCollisionSolving(bool active) => _motor.SetMovementCollisionsSolvingActivation(active);
    public void SetGroundingSolving(bool active) => _motor.SetGroundSolvingActivation(active);

    public Vector3 GetVelocityForMovePosition(Vector3 targetPosition, float deltaTime)
        => _motor.GetVelocityForMovePosition(_motor.TransientPosition, targetPosition, deltaTime);
}
