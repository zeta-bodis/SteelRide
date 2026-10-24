using System;
using UnityEngine;

namespace SteelRide.Vehicle
{
    [DisallowMultipleComponent]
    public sealed class WheelVisualRotation : MonoBehaviour
    {
        [SerializeField] private Transform _motionSource;
        [SerializeField] private Vector3 _localForwardAxis = Vector3.forward;
        [SerializeField] private WheelVisual[] _wheels = Array.Empty<WheelVisual>();

        private Vector3 _previousPosition;
        private Quaternion[] _initialRotations;

        private void Reset()
        {
            _motionSource = transform;
        }

        private void Awake()
        {
            if (_motionSource == null)
                _motionSource = transform;

            ValidateSettings();

            _initialRotations = new Quaternion[_wheels.Length];

            for (int i = 0; i < _wheels.Length; i++)
            {
                Transform pivot = _wheels[i]?.Pivot;
                if (pivot == null)
                {
                    enabled = false;
                    return;
                }

                _initialRotations[i] = pivot.localRotation;
                if (pivot == _motionSource || _motionSource.IsChildOf(pivot))
                {
                    enabled = false;
                    return;
                }

                for (int j = 0; j < i; j++)
                {
                    if (_wheels[j].Pivot != pivot)
                        continue;

                    enabled = false;
                    return;
                }
            }
        }

        private void OnEnable() => ResetMotionSample();

        private void OnValidate() => ValidateSettings();

        public void ResetMotionSample()
        {
            if (_motionSource != null)
                _previousPosition = _motionSource.position;
        }

        public void ResetForRun()
        {
            if (_initialRotations != null)
                for (int i = 0; i < _wheels.Length; i++)
                    if (_wheels[i]?.Pivot != null)
                        _wheels[i].Pivot.localRotation = _initialRotations[i];

            ResetMotionSample();
        }

        private void LateUpdate()
        {
            if (_motionSource == null)
                return;

            Vector3 currentPosition = _motionSource.position;
            Vector3 displacement = currentPosition - _previousPosition;

            _previousPosition = currentPosition;

            Vector3 forward = _motionSource.TransformDirection(_localForwardAxis);
            float signedDistance = Vector3.Dot(displacement, forward);

            for (int i = 0; i < _wheels.Length; i++)
                _wheels[i]?.Rotate(signedDistance);
        }

        private void ValidateSettings()
        {
            _localForwardAxis = _localForwardAxis.sqrMagnitude > 0.0001f ? _localForwardAxis.normalized : Vector3.forward;

            if (_wheels == null)
                _wheels = Array.Empty<WheelVisual>();

            foreach (WheelVisual wheel in _wheels)
                wheel?.Validate();
        }

        [Serializable]
        private sealed class WheelVisual
        {
            [SerializeField] private Transform _pivot;
            [SerializeField, Min(0.001f)] private float _worldRadius = 0.35f;
            [SerializeField] private Vector3 _localRotationAxis = Vector3.right;
            [SerializeField] private bool _reverse;

            public Transform Pivot => _pivot;

            public void Rotate(float signedDistance)
            {
                if (_pivot == null)
                    return;

                float angle = signedDistance / _worldRadius * Mathf.Rad2Deg;

                if (_reverse)
                    angle = -angle;

                _pivot.Rotate(_localRotationAxis, angle, Space.Self);
            }

            public void Validate()
            {
                _worldRadius = Mathf.Max(0.001f, _worldRadius);
                _localRotationAxis = _localRotationAxis.sqrMagnitude > 0.0001f ? _localRotationAxis.normalized : Vector3.right;
            }
        }
    }
}