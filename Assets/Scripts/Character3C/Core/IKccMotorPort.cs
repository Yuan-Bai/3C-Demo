


using UnityEngine;

public interface IKccMotorPort
{
    bool IsGrounded {get;}
    Vector3 GroundNormal {get;}
    Vector3 CharacterUp {get;}
    Vector3 Velocity {get;}

    void ForceUnground(float duration=0.1f);
    void SetCollisionSolving(bool active);
    void SetGroundingSolving(bool active);

    Vector3 GetVelocityForMovePosition(Vector3 targetPosition, float deltaTime);
}