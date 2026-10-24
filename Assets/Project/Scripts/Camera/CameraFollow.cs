using System;
using UnityEngine;

namespace SteelRide.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _smoothTime = 0.12f;

        private Transform _target;
        private Vector3 _offset;
        private Vector3 _velocity;

        public void SetTarget(Transform target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            _target = target;
            _offset = transform.position - target.position;
            _velocity = Vector3.zero;
        }

        public void SnapToTarget()
        {
            _velocity = Vector3.zero;

            if (_target != null)
                transform.position = _target.position + _offset;
        }

        private void OnEnable() => _velocity = Vector3.zero;

        private void OnValidate()
        {
            _smoothTime = Mathf.Max(0.01f, _smoothTime);
        }

        private void LateUpdate()
        {
            if (_target == null || Time.timeScale <= 0f || Time.deltaTime <= 0f)
                return;

            transform.position = Vector3.SmoothDamp(transform.position, _target.position + _offset, ref _velocity, _smoothTime, Mathf.Infinity, Time.deltaTime);
        }
    }
}