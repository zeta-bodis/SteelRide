using System;
using SteelRide.Core;
using UnityEngine;

namespace SteelRide.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _speed = 6f;
        [SerializeField] private Vector3 _worldDirection = Vector3.forward;
        [SerializeField, Min(0f)] private float _swayAmplitude = 1f;
        [SerializeField, Min(0.1f)] private float _swayWavelength = 30f;

        private Rigidbody _body;
        private SinusoidalDriveMotion _motion;
        private Vector3 _sideDirection;
        private IGameStateSource _session;
        private bool _subscribed;
        private bool _isMoving;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;

            ValidateSettings();

            _motion = new SinusoidalDriveMotion(_swayAmplitude, _swayWavelength);
            _startPosition = _body.position;
            _startRotation = _body.rotation;
            _initialized = true;
        }

        public void ResetToStart()
        {
            EnsureInitialized();

            _isMoving = false;
            _motion.Reset();
            _body.position = _startPosition;
            _body.rotation = _startRotation;

            transform.SetPositionAndRotation(_startPosition, _startRotation);
        }

        public void Bind(IGameStateSource session)
        {
            EnsureInitialized();

            if (session == null)
                throw new ArgumentNullException(nameof(session));

            Unbind();

            _session = session;

            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();

            _session = null;
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unbind();

        private void OnValidate() => ValidateSettings();

        private void Subscribe()
        {
            if (_subscribed || _session == null || !isActiveAndEnabled)
                return;

            _session.StateChanged += OnStateChanged;
            _subscribed = true;

            RefreshMovement();
        }

        private void Unsubscribe()
        {
            if (_subscribed && _session != null)
                _session.StateChanged -= OnStateChanged;

            _subscribed = false;
            _isMoving = false;
        }

        private void OnStateChanged(GameState state) => RefreshMovement();

        private void RefreshMovement()
        {
            _isMoving = _session != null && _session.State == GameState.Playing;
        }

        private void FixedUpdate()
        {
            if (!_isMoving || _speed <= 0f)
                return;

            _motion.Advance((double)_speed * Time.fixedDeltaTime);
            Vector3 position = _startPosition + _worldDirection * (float)_motion.Distance + _sideDirection * _motion.LateralOffset;
            _body.MovePosition(position);
            Vector3 heading = _worldDirection + _sideDirection * _motion.LateralSlope;
            heading.y = 0f;

            if (heading.sqrMagnitude > 0.0001f)
                _body.MoveRotation(Quaternion.LookRotation(heading, Vector3.up));
        }

        private void ValidateSettings()
        {
            _speed = FiniteNonNegative(_speed, 6f);
            _swayAmplitude = FiniteNonNegative(_swayAmplitude, 1f);
            _swayWavelength = Mathf.Max(0.1f, FiniteNonNegative(_swayWavelength, 30f));
            _worldDirection = _worldDirection.sqrMagnitude > 0.0001f ? _worldDirection.normalized : Vector3.forward;
            _sideDirection = Vector3.Cross(Vector3.up, _worldDirection).normalized;
            _motion?.Configure(_swayAmplitude, _swayWavelength);
        }

        private static float FiniteNonNegative(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0f, value);
    }
}