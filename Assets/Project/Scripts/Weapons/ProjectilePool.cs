using System;
using System.Collections.Generic;
using SteelRide.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace SteelRide.Weapons
{
    [DisallowMultipleComponent]
    public sealed class ProjectilePool : MonoBehaviour, IProjectileSpawner
    {
        [SerializeField] private Projectile _prefab;
        [SerializeField] private LayerMask _hitMask = Physics.DefaultRaycastLayers;

        [SerializeField, Min(1)] private int _maxActive = 64;
        [SerializeField, Min(1)] private int _maxRetained = 64;

        private readonly List<Projectile> _active = new List<Projectile>();
        private ObjectPool<Projectile> _pool;
        private IGameStateSource _session;
        private bool _subscribed;
        private Action<Projectile> _returnHandler;
        private Action<ProjectileImpact> _impactHandler;
        public event Action<ProjectileImpact> Impact;

        private void Awake()
        {
            OnValidate();

            if (_prefab == null)
            {
                enabled = false;
                return;
            }

            _returnHandler = ReturnToPool;
            _impactHandler = impact => Impact?.Invoke(impact);

            _pool = new ObjectPool<Projectile>(CreateProjectile, actionOnGet: null, actionOnRelease: projectile => projectile.Despawn(), actionOnDestroy: projectile => Destroy(projectile.gameObject), collectionCheck: true, defaultCapacity: _maxRetained, maxSize: _maxRetained);
            _active.Capacity = _maxActive;

            var warmed = new Projectile[_maxActive];

            for (int i = 0; i < warmed.Length; i++)
                warmed[i] = _pool.Get();

            for (int i = 0; i < warmed.Length; i++)
                _pool.Release(warmed[i]);
        }

        public void Bind(IGameStateSource session)
        {
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

        private void OnDestroy()
        {
            Unbind();

            _pool?.Dispose();
            _pool = null;
        }

        private void Subscribe()
        {
            if (_subscribed || _session == null || !isActiveAndEnabled)
                return;

            _session.StateChanged += OnStateChanged;
            _subscribed = true;

            OnStateChanged(_session.State);
        }

        private void Unsubscribe()
        {
            if (_subscribed && _session != null)
                _session.StateChanged -= OnStateChanged;

            _subscribed = false;

            ClearActive();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Playing || state == GameState.Paused)
            {
                for (int i = 0; i < _active.Count; i++)
                    _active[i].SetPaused(state == GameState.Paused);
            }
            else
            {
                ClearActive();
            }
        }

        public void ResetForRun() => ClearActive();

        public bool TrySpawn(ProjectileSpawnData data)
        {
            if (!isActiveAndEnabled || _pool == null || _session == null || _session.State != GameState.Playing || _active.Count >= _maxActive || data.Owner == null)
                return false;

            Projectile projectile = _pool.Get();
            _active.Add(projectile);

            projectile.Launch(data, _hitMask, _returnHandler, _impactHandler);
            return true;
        }

        private Projectile CreateProjectile()
        {
            Projectile projectile = Instantiate(_prefab, transform);
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        private void ReturnToPool(Projectile projectile)
        {
            if (!_active.Remove(projectile))
                return;

            _pool.Release(projectile);
        }

        private void ClearActive()
        {
            while (_active.Count > 0)
                ReturnToPool(_active[_active.Count - 1]);
        }

        private void OnValidate()
        {
            _maxActive = Mathf.Max(1, _maxActive);
            _maxRetained = Mathf.Max(_maxActive, _maxRetained);
        }
    }
}