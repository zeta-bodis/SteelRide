using System;
using SteelRide.Combat;
using SteelRide.Core;
using SteelRide.Enemies;
using UnityEngine;
using Unity.Profiling;

namespace SteelRide.Spawning
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(300)]
    public sealed class LevelStreamer : MonoBehaviour
    {
        private static readonly ProfilerMarker PrewarmMarker = new ProfilerMarker("LevelStreamer.Prewarm");
        private static readonly ProfilerMarker TickMarker = new ProfilerMarker("LevelStreamer.Tick");
        private static readonly ProfilerMarker SpawnEnemiesMarker = new ProfilerMarker("LevelStreamer.SpawnEnemies");
        private static readonly ProfilerMarker RecycleMarker = new ProfilerMarker("LevelStreamer.Recycle");

        [Header("Prefabs and straight +Z level")]
        [SerializeField]
        private GroundSegment _groundPrefab;
        [SerializeField]
        private PooledEnemy _enemyPrefab;
        [SerializeField]
        private Transform _levelOrigin;
        [SerializeField, Min(1)]
        private int _segmentCount = 20;
        [SerializeField, Min(0.1f)]
        private float _finishInset = 5f;
        [Header("Fixed prewarmed capacities")]
        [SerializeField, Min(1)]
        private int _groundCapacity = 12;
        [SerializeField, Min(1)]
        private int _enemyCapacity = 64;
        [Header("Streaming window and budgets")]
        [SerializeField, Min(1f)]
        private float _spawnAhead = 120f;
        [SerializeField, Min(1f)]
        private float _keepBehind = 15f;
        [SerializeField, Min(0f)]
        private float _behindCameraMargin = 3f;
        [SerializeField, Min(0.02f)]
        private float _updateInterval = 0.1f;
        [SerializeField, Min(1)]
        private int _maxGroundPerTick = 1;
        [SerializeField, Min(1)]
        private int _maxEnemyAttemptsPerTick = 4;
        [SerializeField, Min(1)]
        private int _maxRecyclesPerTick = 8;
        [Header("Enemy layout on each segment")]
        [SerializeField, Min(0)]
        private int _enemiesPerSegment = 6;
        [SerializeField]
        private int _seed = 12345;
        [SerializeField, Min(0f)]
        private float _edgeMargin = 1f;
        [SerializeField, Min(0.1f)]
        private float _minimumSpacing = 3f;
        [SerializeField, Min(1f)]
        private float _safeSpawnAhead = 25f;
        [SerializeField]
        private float _enemyGroundOffset;
        [SerializeField, Min(1f)]
        private float _surfaceProbeHeight = 20f;
        private FixedPool<GroundSegment> _ground;
        private FixedPool<PooledEnemy> _enemies;
        private Transform _stagingRoot, _groundRoot, _enemyRoot;
        private IGameStateSource _session;
        private IGameOutcome _outcome;
        private HealthComponent _vehicle;
        private Transform _camera;
        private Vector3 _origin;
        private float _length;
        private float _nextTick;
        private int _nextSegment;
        public float FinishZ => _origin.z + _segmentCount * _length - _finishInset;

        public event Action<EnemyContactImpact> EnemyContact;
        public void Bind(IGameStateSource session, IGameOutcome outcome, HealthComponent vehicle, Transform cameraTransform)
        {
            if (session == null || outcome == null || vehicle == null || cameraTransform == null)
                throw new ArgumentNullException(nameof(session));

            Unbind();
            ValidateConfiguration(vehicle, cameraTransform);

            _session = session;
            _outcome = outcome;
            _vehicle = vehicle;
            _camera = cameraTransform;
            _origin = _levelOrigin.position;
            _length = _groundPrefab.Length;

            if (_ground == null)
                Prewarm();
            var target = new EnemyTargetContext(vehicle);

            for (int i = 0; i < _enemies.Capacity; i++)
                _enemies.ItemAt(i).Bind(session, target);

            InitializeWindow();
        }

        public void ResetForRun()
        {
            if (_session == null)
                throw new InvalidOperationException("Bind the streamer before resetting.");

            for (int i = 0; i < _enemies.Capacity; i++)
                if (_enemies.IsRented(i))
                {
                    _enemies.ItemAt(i).Despawn();
                    _enemies.TryReturn(i);
                }

            for (int i = 0; i < _ground.Capacity; i++)
                if (_ground.IsRented(i))
                {
                    _ground.ItemAt(i).gameObject.SetActive(false);
                    _ground.TryReturn(i);
                }

            InitializeWindow();
        }

        private void InitializeWindow()
        {
            float startZ = Math.Min(_vehicle.transform.position.z, _camera.position.z) - _keepBehind;
            _nextSegment = Math.Max(0, (int)Math.Floor((startZ - _origin.z) / _length));

            SpawnGround(_ground.Capacity);
            SpawnEnemies(_ground.Capacity * _enemiesPerSegment);

            _nextTick = Time.unscaledTime + _updateInterval;
        }

        private void ValidateConfiguration(HealthComponent vehicle, Transform cameraTransform)
        {
            if (_groundPrefab == null || _enemyPrefab == null || _levelOrigin == null)
                throw new InvalidOperationException("LevelStreamer: assign Ground Prefab, Enemy Prefab and Level Origin.");

            if (_groundCapacity < 1 || _enemyCapacity < 1 || _segmentCount < 1 || _updateInterval < 0.02f || _maxGroundPerTick < 1 || _maxEnemyAttemptsPerTick < 1 || _maxRecyclesPerTick < 1 || _spawnAhead <= _safeSpawnAhead || _keepBehind < 0f || _finishInset <= 0f || _finishInset >= _groundPrefab.Length || _enemiesPerSegment < 0 || _behindCameraMargin < 0f || _surfaceProbeHeight <= 0f)
                throw new InvalidOperationException("LevelStreamer: invalid capacities, budgets or distances.");

            if (transform.lossyScale != Vector3.one || _groundPrefab.transform.localScale != Vector3.one || _enemyPrefab.transform.localScale != Vector3.one)
                throw new InvalidOperationException("Streamer and prefab roots need scale (1,1,1). Scale the child models.");

            if (vehicle.transform.position.z < _levelOrigin.position.z || vehicle.transform.position.z >= _levelOrigin.position.z + _segmentCount * _groundPrefab.Length - _finishInset)
                throw new InvalidOperationException("Place the vehicle inside the finite level, before its finish.");

            float tailZ = Math.Min(vehicle.transform.position.z, cameraTransform.position.z) - _keepBehind;
            int first = Math.Max(0, (int)Math.Floor((tailZ - _levelOrigin.position.z) / _groundPrefab.Length));
            int last = Math.Min(_segmentCount - 1, (int)Math.Floor((vehicle.transform.position.z + _spawnAhead - _levelOrigin.position.z) / _groundPrefab.Length));

            if (_groundCapacity < last - first + 2)
                throw new InvalidOperationException("Ground Capacity must fit the initial window plus one spare segment.");

            _ = MakeLayout(0);
        }

        private void Prewarm()
        {
            using (PrewarmMarker.Auto())
            {
                _stagingRoot = NewRoot("Inactive staging");
                _stagingRoot.gameObject.SetActive(false);
                _groundRoot = NewRoot("Ground pool");
                _enemyRoot = NewRoot("Enemy pool");
                _ground = new FixedPool<GroundSegment>(_groundCapacity, CreateGround);
                _enemies = new FixedPool<PooledEnemy>(_enemyCapacity, CreateEnemy);
            }
        }

        private Transform NewRoot(string objectName)
        {
            var root = new GameObject(objectName).transform;
            root.SetParent(transform, false);
            return root;
        }

        private GroundSegment CreateGround(int index)
        {
            GroundSegment item = Instantiate(_groundPrefab, _stagingRoot);
            item.gameObject.SetActive(false);
            item.transform.SetParent(_groundRoot, false);
            item.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            item.gameObject.SetActive(true);
            Physics.SyncTransforms();
            item.Initialize();
            item.gameObject.SetActive(false);
            return item;
        }

        private PooledEnemy CreateEnemy(int index)
        {
            PooledEnemy item = Instantiate(_enemyPrefab, _stagingRoot);
            item.gameObject.SetActive(false);
            item.transform.SetParent(_enemyRoot, false);
            item.gameObject.SetActive(true);
            item.Despawn();
            item.Controller.VehicleHit += OnEnemyContact;
            return item;
        }

        private void OnEnemyContact(EnemyContactImpact impact) => EnemyContact?.Invoke(impact);

        private EnemySpawnLayout MakeLayout(int segment)
        {
            return new EnemySpawnLayout(_groundPrefab.Width, _groundPrefab.Length, _edgeMargin, _minimumSpacing, _enemiesPerSegment, _seed, segment);
        }

        private void LateUpdate()
        {
            if (_session == null || _session.State == GameState.Paused || Time.unscaledTime < _nextTick)
                return;

            _nextTick = Time.unscaledTime + _updateInterval;

            using (TickMarker.Auto())
            {
                RecycleBehindCamera(_maxRecyclesPerTick);

                if (_session.State != GameState.Playing && _session.State != GameState.Ready)
                    return;

                if (_session.State == GameState.Playing && _vehicle.transform.position.z >= FinishZ)
                {
                    _outcome.TryWin();
                    return;
                }

                SpawnGround(_maxGroundPerTick);
                SpawnEnemies(_maxEnemyAttemptsPerTick);
            }
        }

        private void SpawnGround(int budget)
        {
            bool changed = false;
            float aheadZ = _vehicle.transform.position.z + _spawnAhead;

            while (budget-- > 0 && _nextSegment < _segmentCount && _origin.z + _nextSegment * _length <= aheadZ)
            {
                if (!_ground.TryRent(out GroundSegment tile, out _))
                    break;

                tile.Place(_nextSegment, _origin + Vector3.forward * ((_nextSegment + 0.5f) * _length), MakeLayout(_nextSegment));

                _nextSegment++;

                changed = true;
            }

            if (changed)
                Physics.SyncTransforms();
        }

        private void SpawnEnemies(int attemptBudget)
        {
            using (SpawnEnemiesMarker.Auto())
            {
                float earliestZ = _vehicle.transform.position.z + _safeSpawnAhead;
                for (int i = 0; i < _ground.Capacity && attemptBudget > 0; i++)
                {
                    if (!_ground.IsRented(i))
                        continue;

                    GroundSegment tile = _ground.ItemAt(i);
                    while (tile.NextEnemy < tile.Layout.Count && attemptBudget-- > 0)
                    {
                        tile.Layout.GetPosition(tile.NextEnemy, out float x, out float z);
                        Vector3 point = _origin + new Vector3(x, 0f, tile.SegmentIndex * _length + z);

                        if (point.z < earliestZ || point.z > FinishZ - _edgeMargin)
                        {
                            tile.NextEnemy++;
                            continue;
                        }

                        if (_enemies.AvailableCount == 0)
                            return;

                        tile.NextEnemy++;

                        if (!tile.TryFindSurface(point, _surfaceProbeHeight, out Vector3 surface))
                            continue;

                        if (_enemies.TryRent(out PooledEnemy enemy, out _))
                            enemy.Spawn(surface + Vector3.up * _enemyGroundOffset, Quaternion.Euler(0f, (tile.NextEnemy * 137 + tile.SegmentIndex * 31) % 360, 0f));
                    }
                }
            }
        }

        private void RecycleBehindCamera(int budget)
        {
            using (RecycleMarker.Auto())
            {
                Vector3 cameraPosition = _camera.position;
                Vector3 forward = _camera.forward;
                float tailZ = _vehicle.transform.position.z - _keepBehind;

                for (int i = 0; i < _enemies.Capacity && budget > 0; i++)
                {
                    if (!_enemies.IsRented(i))
                        continue;

                    PooledEnemy enemy = _enemies.ItemAt(i);
                    Vector3 position = enemy.transform.position;
                    if (position.z + enemy.CleanupRadius >= tailZ || !BehindCamera.ContainsEntireSphere(position, enemy.CleanupRadius, cameraPosition, forward, _behindCameraMargin))
                        continue;

                    enemy.Despawn();

                    _enemies.TryReturn(i);
                    budget--;
                }

                for (int i = 0; i < _ground.Capacity && budget > 0; i++)
                {
                    if (!_ground.IsRented(i))
                        continue;

                    GroundSegment tile = _ground.ItemAt(i);

                    if (tile.VisualBounds.max.z >= tailZ || !BehindCamera.ContainsEntireBounds(tile.VisualBounds, cameraPosition, forward, _behindCameraMargin))
                        continue;

                    tile.gameObject.SetActive(false);
                    _ground.TryReturn(i);

                    budget--;
                }
            }
        }

        public void Unbind()
        {
            if (_enemies != null)
            {
                for (int i = 0; i < _enemies.Capacity; i++)
                {
                    PooledEnemy enemy = _enemies.ItemAt(i);

                    if (enemy == null)
                        continue;

                    if (_enemies.IsRented(i))
                    {
                        enemy.Despawn();
                        _enemies.TryReturn(i);
                    }

                    enemy.Unbind();
                }
            }

            if (_ground != null)
            {
                for (int i = 0; i < _ground.Capacity; i++)
                {
                    if (_ground.IsRented(i) && _ground.ItemAt(i) != null)
                    {
                        _ground.ItemAt(i).gameObject.SetActive(false);
                        _ground.TryReturn(i);
                    }
                }
            }

            _session = null;
            _outcome = null;
            _vehicle = null;
            _camera = null;
        }

        private void OnDestroy()
        {
            Unbind();

            if (_enemies == null)
                return;

            for (int i = 0; i < _enemies.Capacity; i++)
                if (_enemies.ItemAt(i) != null && _enemies.ItemAt(i).Controller != null)
                    _enemies.ItemAt(i).Controller.VehicleHit -= OnEnemyContact;
        }
    }
}