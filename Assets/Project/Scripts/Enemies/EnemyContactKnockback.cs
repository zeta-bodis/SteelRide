using System;
using UnityEngine;

namespace SteelRide.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController), typeof(Rigidbody))]
    public sealed class EnemyContactKnockback : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _extraDistance = 1f;
        [SerializeField, Min(0f)] private float _clearance = 0.2f;
        [SerializeField, Min(0f)] private float _height = 0.35f;
        [SerializeField, Min(0.05f)] private float _duration = 0.35f;

        private EnemyController _enemy;
        private Rigidbody _body;
        private ContactKnockbackTrajectory _trajectory;
        private float _elapsed;
        private float _flightDuration;
        private bool _hasPlayed;
        private bool _isFlying;
        private Action<EnemyContactImpact> _hitHandler;

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _body = GetComponent<Rigidbody>();
            _hitHandler = OnVehicleHit;
        }

        private void OnEnable() => _enemy.VehicleHit += _hitHandler;

        private void OnDisable()
        {
            _enemy.VehicleHit -= _hitHandler;

            if (!_isFlying)
                return;

            _isFlying = false;

            if (_body != null)
                _body.position = _trajectory.End;
        }

        private void OnVehicleHit(EnemyContactImpact impact)
        {
            if (_hasPlayed || _enemy.State != EnemyState.Dead)
                return;

            _trajectory = ContactKnockbackTrajectory.Create(impact, _extraDistance, _clearance, _height);
            _flightDuration = Mathf.Max(0.05f, _duration);
            _elapsed = 0f;
            _hasPlayed = true;
            _isFlying = true;
        }

        private void FixedUpdate()
        {
            if (!_isFlying)
                return;

            _elapsed = Mathf.Min(_flightDuration, _elapsed + Time.fixedDeltaTime);
            _body.MovePosition(_trajectory.PositionAt(_elapsed / _flightDuration));

            if (_elapsed >= _flightDuration)
                _isFlying = false;
        }

        private void OnValidate()
        {
            _extraDistance = Mathf.Max(0f, _extraDistance);
            _clearance = Mathf.Max(0f, _clearance);
            _height = Mathf.Max(0f, _height);
            _duration = Mathf.Max(0.05f, _duration);
        }

        public void CancelForPool() => _isFlying = false;

        public void ResetForSpawn()
        {
            _isFlying = false;
            _hasPlayed = false;
            _elapsed = 0f;
            _trajectory = default;
        }
    }
}