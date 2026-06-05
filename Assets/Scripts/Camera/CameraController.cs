using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float zoomSpeed = 1.0f;
    [SerializeField] [Min(0.01f)] private float zoomSmoothTime = 0.12f;
    [SerializeField] [Range(0.5f, 4f)] private float minDistance = 2.5f;
    [SerializeField] [Range(4f, 10f)] private float maxDistance = 7f;

    private float _targetDistance;
    private float _zoomVelocity;
    private CinemachineFramingTransposer _vcamBody;
    [SerializeField] private PlayerInputReader _input;


    private void Awake()
    {
        _vcamBody = GetComponent<CinemachineVirtualCamera>()?.GetCinemachineComponent<CinemachineFramingTransposer>();
    
        if (_vcamBody != null)
        {
            _targetDistance = _vcamBody.m_CameraDistance;
        }
    }

    private void LateUpdate()
    {
        if (_vcamBody == null || _input == null) return;

        var zoomDelta = _input.ZoomDelta;
        if (Mathf.Abs(zoomDelta) > Mathf.Epsilon)
        {
            _targetDistance = Mathf.Clamp(_targetDistance - zoomDelta * zoomSpeed * Time.deltaTime, minDistance, maxDistance);
        }

        _vcamBody.m_CameraDistance = Mathf.SmoothDamp(
            _vcamBody.m_CameraDistance,
            _targetDistance,
            ref _zoomVelocity,
            zoomSmoothTime);
    }
}
