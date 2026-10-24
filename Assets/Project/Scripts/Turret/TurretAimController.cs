using System;
using SteelRide.Aiming;
using SteelRide.Core;
using UnityEngine;

namespace SteelRide.Turret
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class TurretAimController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _turnSpeed = 180f;
        [SerializeField, Range(-179f, 0f)] private float _minYaw = -80f;
        [SerializeField, Range(0f, 179f)] private float _maxYaw = 80f;

        private IGameStateSource _session;
        private IPointerPositionSource _input;
        private IAimPointProvider _aimPoints;
        private YawMotor _motor;
        private Quaternion _restLocalRotation;
        private bool _subscribed;
        private bool _canAim;

        private void Awake()
        {
            ValidateSettings();

            _restLocalRotation = transform.localRotation;
            _motor = new YawMotor(_minYaw, _maxYaw, _turnSpeed);
        }

        public void Bind(IGameStateSource session, IPointerPositionSource input, IAimPointProvider aimPoints)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (aimPoints == null)
                throw new ArgumentNullException(nameof(aimPoints));

            Unbind();

            _session = session;
            _input = input;
            _aimPoints = aimPoints;

            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();

            _session = null;
            _input = null;
            _aimPoints = null;
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

            RefreshState();
        }

        private void Unsubscribe()
        {
            if (_subscribed && _session != null)
                _session.StateChanged -= OnStateChanged;

            _subscribed = false;
            _canAim = false;
        }

        private void OnStateChanged(GameState state) => RefreshState();

        private void RefreshState()
        {
            _canAim = _session != null && _session.State == GameState.Playing;
        }

        private void LateUpdate()
        {
            if (!_canAim || !_input.TryGetPosition(out Vector2 screenPosition) || !_aimPoints.TryGetAimPoint(screenPosition, out Vector3 worldPoint))
                return;

            Quaternion referenceRotation = transform.parent != null ? transform.parent.rotation * _restLocalRotation : _restLocalRotation;
            Vector3 localDirection = Quaternion.Inverse(referenceRotation) * (worldPoint - transform.position);

            if (localDirection.x * localDirection.x + localDirection.z * localDirection.z < 0.0001f)
                return;

            float requestedYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float yaw = _motor.StepTowards(requestedYaw, Time.deltaTime);

            transform.localRotation = _restLocalRotation * Quaternion.AngleAxis(yaw, Vector3.up);
        }

        public void ResetForRun()
        {
            _motor?.Reset();
            transform.localRotation = _restLocalRotation;
        }

        private void ValidateSettings()
        {
            _turnSpeed = Mathf.Max(0f, _turnSpeed);
            _minYaw = Mathf.Clamp(_minYaw, -179f, 0f);
            _maxYaw = Mathf.Clamp(_maxYaw, 0f, 179f);
        }
    }
}