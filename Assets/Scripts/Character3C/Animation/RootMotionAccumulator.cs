

using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class RootMotionAccumulator : MonoBehaviour
{
    private Animator _animator;
    private Vector3 _deltaPosition;
    private Quaternion _deltaRotation = Quaternion.identity;

    private void Awake()
    {
        _animator ??= GetComponent<Animator>();
    }

    private void OnAnimatorMove()
    {
        _deltaPosition += _animator.deltaPosition;
        _deltaRotation = _animator.deltaRotation * _deltaRotation;
    }

    public void ConsumeVelocity(bool grounded, Vector3 groundNormal, float deltaTime, ref Vector3 currentVelocity)
    {
        if (deltaTime <= 0f) return;
        
        if (grounded)
        {
            currentVelocity = Vector3.ProjectOnPlane(_deltaPosition, groundNormal) / deltaTime;
        }
        else
        {
            currentVelocity = _deltaPosition / deltaTime;
        }

        _deltaPosition = Vector3.zero;
    }

    public void ConsumeRotation(float deltaTime, ref Quaternion currentRotation)
    {
        if (deltaTime <= 0f) return;

        currentRotation = _deltaRotation * currentRotation;
        _deltaRotation = Quaternion.identity;
    }
}