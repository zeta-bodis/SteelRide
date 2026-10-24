using System;
using SteelRide.Combat;
using SteelRide.Core;
using UnityEngine;

namespace SteelRide.Weapons
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class AutomaticWeapon : MonoBehaviour
    {
        [SerializeField] private Transform _firePoint;
        [SerializeField, Min(0.1f)] private float _shotsPerSecond = 6f;
        [SerializeField, Min(1)] private int _damage = 25;
        [SerializeField, Min(0.1f)] private float _projectileSpeed = 45f;
        [SerializeField, Min(0.01f)] private float _projectileLifetime = 3f;
        [SerializeField, Min(0.001f)] private float _projectileRadius = 0.08f;

        private IGameStateSource _session;
        private IProjectileSpawner _spawner;
        private Transform _owner;
        private FireCooldown _cooldown;
        private bool _subscribed;
        private bool _canFire;

        public event Action ShotFired;

        private void Awake()
        {
            ValidateSettings();

            _cooldown = new FireCooldown(_shotsPerSecond);

            if (_firePoint == null)
                enabled = false;
        }

        public void Bind(IGameStateSource session, IProjectileSpawner spawner, Transform owner)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (spawner == null)
                throw new ArgumentNullException(nameof(spawner));

            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            Unbind();

            _session = session;
            _spawner = spawner;
            _owner = owner;

            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();

            _session = null;
            _spawner = null;
            _owner = null;
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
            _canFire = false;
        }

        private void OnStateChanged(GameState state) => RefreshState();

        private void RefreshState()
        {
            _canFire = _session != null && _session.State == GameState.Playing;

            if (_session == null || _session.State == GameState.Ready || _session.State == GameState.Won || _session.State == GameState.Lost)
                _cooldown?.Reset();
        }

        private void LateUpdate()
        {
            if (!_canFire || _owner == null || _firePoint == null)
                return;

            double time = Time.timeAsDouble;
            if (!_cooldown.IsReady(time))
                return;

            var data = new ProjectileSpawnData(_firePoint.position, _firePoint.forward, _owner, _damage, _projectileSpeed, _projectileLifetime, _projectileRadius);
            if (!_spawner.TrySpawn(data))
                return;

            _cooldown.MarkFired(time);

            ShotFired?.Invoke();
        }

        private void ValidateSettings()
        {
            _shotsPerSecond = Mathf.Max(0.1f, _shotsPerSecond);
            _damage = Mathf.Max(1, _damage);
            _projectileSpeed = Mathf.Max(0.1f, _projectileSpeed);
            _projectileLifetime = Mathf.Max(0.01f, _projectileLifetime);
            _projectileRadius = Mathf.Max(0.001f, _projectileRadius);
        }
    }
}