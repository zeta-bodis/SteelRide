using System;
using SteelRide.Combat;
using SteelRide.Core;
using UnityEngine;

namespace SteelRide.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent), typeof(EnemyMovement))]
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _activationRadius = 18f;

        private readonly EnemyBrain _brain = new EnemyBrain();
        private HealthComponent _health;
        private EnemyMovement _movement;
        private Collider[] _colliders;
        private bool[] _colliderDefaults;
        private Action _diedHandler;
        private Action<EnemyState> _brainHandler;
        private Action<GameState> _sessionHandler;
        private HealthComponent _vehicleHealth;
        private Rigidbody _vehicleBody;
        private Collider[] _vehicleColliders;
        private IGameStateSource _session;
        private bool _sessionSubscribed;
        private bool _retainSessionSubscription;

        public EnemyState State => _brain.State;

        public event Action<EnemyState> StateChanged;
        public event Action<EnemyContactImpact> VehicleHit;

        private bool CanAct => isActiveAndEnabled && _session != null && _session.State == GameState.Playing && _vehicleHealth != null && _vehicleHealth.isActiveAndEnabled && _vehicleHealth.IsAlive;
        private Vector3 TargetPosition => _vehicleBody != null ? _vehicleBody.position : _vehicleHealth.transform.position;

        private void Awake() => EnsureComponents();

        private void EnsureComponents()
        {
            if (_health != null)
                return;

            _health = GetComponent<HealthComponent>();
            _movement = GetComponent<EnemyMovement>();
            _colliders = GetComponentsInChildren<Collider>(true);
            _colliderDefaults = new bool[_colliders.Length];

            for (int i = 0; i < _colliders.Length; i++)
                _colliderDefaults[i] = _colliders[i].enabled;

            _diedHandler = OnDied;
            _brainHandler = OnBrainStateChanged;
            _sessionHandler = OnGameStateChanged;
        }

        public void Bind(IGameStateSource session, EnemyTargetContext target, bool retainSessionSubscription = false)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Unbind();

            _session = session;
            _retainSessionSubscription = retainSessionSubscription;
            _vehicleHealth = target.Health;
            _vehicleBody = target.Body;
            _vehicleColliders = target.Colliders;

            SubscribeSession();
            Refresh();
        }

        public void Unbind()
        {
            UnsubscribeSession();

            _session = null;
            _vehicleHealth = null;
            _vehicleBody = null;
            _vehicleColliders = null;

            Refresh();
        }

        private void OnEnable()
        {
            EnsureComponents();

            _health.Died += _diedHandler;
            _brain.StateChanged += _brainHandler;

            SubscribeSession();
            Refresh();
            ApplyState(State);
        }

        private void OnDisable()
        {
            if (!_retainSessionSubscription)
                UnsubscribeSession();

            _brain.Refresh(false, _health.IsAlive, false);
            _movement.Stop();
            _health.Died -= _diedHandler;
            _brain.StateChanged -= _brainHandler;
        }

        private void SubscribeSession()
        {
            if (_sessionSubscribed || _session == null || (!isActiveAndEnabled && !_retainSessionSubscription))
                return;

            _session.StateChanged += _sessionHandler;
            _sessionSubscribed = true;
        }

        private void UnsubscribeSession()
        {
            if (_sessionSubscribed && _session != null)
                _session.StateChanged -= _sessionHandler;

            _sessionSubscribed = false;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.Paused)
                Refresh();
        }

        private void OnDestroy() => UnsubscribeSession();

        private void Refresh()
        {
            EnsureComponents();

            bool canAct = CanAct;
            bool inRange = false;

            if (canAct)
            {
                Vector3 offset = TargetPosition - _movement.Position;
                offset.y = 0f;
                inRange = offset.sqrMagnitude <= _activationRadius * _activationRadius;
            }

            _brain.Refresh(canAct, _health.IsAlive, inRange);
        }

        private void FixedUpdate()
        {
            if (State == EnemyState.Dead)
                return;

            Refresh();

            if (CanAct && State == EnemyState.Chasing)
                _movement.MoveTowards(TargetPosition, Time.fixedDeltaTime);
        }

        private void OnDied() => _brain.Refresh(false, false, false);

        private void OnBrainStateChanged(EnemyState state)
        {
            ApplyState(state);
            StateChanged?.Invoke(state);
        }

        private void ApplyState(EnemyState state)
        {
            if (state != EnemyState.Chasing)
                _movement.Stop();

            if (state != EnemyState.Dead)
                return;

            foreach (Collider enemyCollider in _colliders)
                if (enemyCollider != null)
                    enemyCollider.enabled = false;
        }

        private void OnTriggerEnter(Collider other) => TryContact(other);

        private void OnTriggerStay(Collider other) => TryContact(other);

        private void TryContact(Collider other)
        {
            if (!CanAct || !_health.isActiveAndEnabled || !_health.IsAlive || _brain.HasHitVehicle || other == null || other.isTrigger || other.GetComponentInParent<HealthComponent>() != _vehicleHealth)
                return;

            Bounds enemyBounds = CaptureBounds(_colliders, _movement.Position);
            CaptureContact(other, enemyBounds.center, out Vector3 contactPosition, out Vector3 contactNormal);
            var impact = new EnemyContactImpact(_movement.Position, enemyBounds, CaptureBounds(_vehicleColliders, TargetPosition), _vehicleHealth.transform.right, contactPosition, contactNormal);

            if (!_brain.TryHitVehicle(CanAct, _health.IsAlive, _vehicleHealth))
                return;

            if (_health.IsAlive)
                _health.TryApplyDamage(_health.CurrentHealth);

            VehicleHit?.Invoke(impact);
        }

        private static void CaptureContact(Collider vehicleCollider, Vector3 enemyCenter, out Vector3 position, out Vector3 normal)
        {
            Bounds bounds = vehicleCollider.bounds;
            Vector3 direction = enemyCenter - bounds.center;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                direction = vehicleCollider.transform.right;

            direction.Normalize();
            Vector3 outside = bounds.center + direction * (bounds.extents.magnitude + 0.1f);
            position = vehicleCollider.ClosestPoint(outside);
            normal = outside - position;
            normal = normal.sqrMagnitude > 0.0001f ? normal.normalized : direction;
        }

        private static Bounds CaptureBounds(Collider[] colliders, Vector3 fallbackPosition)
        {
            Bounds result = new Bounds(fallbackPosition, Vector3.zero);
            bool found = false;

            if (colliders == null)
                return result;

            foreach (Collider candidate in colliders)
            {
                if (candidate == null || !candidate.enabled || candidate.isTrigger || !candidate.gameObject.activeInHierarchy)
                    continue;

                if (found)
                    result.Encapsulate(candidate.bounds);
                else
                    result = candidate.bounds;

                found = true;
            }

            return result;
        }

        private void OnValidate() => _activationRadius = Mathf.Max(0f, _activationRadius);

        public void ResetForSpawn()
        {
            if (gameObject.activeInHierarchy)
                throw new InvalidOperationException("Reset an enemy only while it is inactive.");

            EnsureComponents();

            _health.ResetForSpawn();
            _brain.ResetForNewLife();

            for (int i = 0; i < _colliders.Length; i++)
                _colliders[i].enabled = _colliderDefaults[i];
        }
    }
}
