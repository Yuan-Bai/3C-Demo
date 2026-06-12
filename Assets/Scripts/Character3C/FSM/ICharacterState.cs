

using UnityEngine;

public interface ICharacterState
{
    CharacterStateId Id {get;}
    StatePriority Priority {get;}

    bool CanEnter(in StateChangeRequest request);
    bool CanExit(in StateChangeRequest request);

    void Enter(in StateChangeRequest request);
    void Exit(in StateChangeRequest request);

    void Tick(float deltaTime);
    
    // KCC钩子
    void BeforeCharacterUpdate(float deltaTime);
    void PostGroundingUpdate(float deltaTime);
    void UpdateRotation(ref Quaternion currentRotation, float deltaTime);
    void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime);
    void AfterCharacterUpdate(float deltaTime);
}