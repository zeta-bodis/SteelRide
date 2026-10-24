using System;
using SteelRide.Core;
using UnityEngine;

namespace SteelRide.Effects
{
    [DisallowMultipleComponent]
    public sealed class VehicleDefeatSequence : MonoBehaviour, IDefeatPresentation
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _explosionPoint;
        [SerializeField] private ParticleEffect _explosion;
        [SerializeField, Min(0.1f)] private float _flipDuration = 1.1f;
        [SerializeField, Min(0f)] private float _explosionDelay = 0.65f;
        [SerializeField, Min(0f)] private float _minimumExplosionTime = 1.4f;
        [SerializeField, Min(0.1f)] private float _maximumDuration = 6f;
        [SerializeField, Min(0f)] private float _hopHeight = 0.8f;
        [SerializeField] private float _sideDistance = 0.8f;
        [SerializeField] private float _rollDegrees = -180f;

        private IGameStateSource _session;
        private DefeatTimeline _timeline;
        private Vector3 _restPosition, _scaledPivot;
        private Quaternion _restRotation;
        private bool _initialized, _playing;

        public bool IsComplete { get; private set; }

        public event Action Completed;

        public void Bind(IGameStateSource session, Transform vehicle, Transform turret)
        {
            Unbind();

            if (!isActiveAndEnabled || _visualRoot == null || _explosion == null || _explosionPoint == null || _visualRoot == vehicle || !_visualRoot.IsChildOf(vehicle) || !turret.IsChildOf(_visualRoot))
                throw new InvalidOperationException("DefeatSequence: Visual Root must be an active child of Player containing the body, wheels and turret.");

            if (_visualRoot.GetComponentsInChildren<Rigidbody>(true).Length > 0 || _visualRoot.GetComponentsInChildren<Collider>(true).Length > 0)
                throw new InvalidOperationException("Keep vehicle Rigidbody and Colliders outside Visual Root.");

            if (!_visualRoot.gameObject.activeInHierarchy)
                throw new InvalidOperationException("DefeatSequence Visual Root must be active.");

            if (!_initialized)
            {
                _restPosition = _visualRoot.localPosition;
                _restRotation = _visualRoot.localRotation;

                Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = new Bounds(_visualRoot.position, Vector3.zero);
                bool found = false;

                foreach (Renderer renderer in renderers)
                {
                    if (renderer is ParticleSystemRenderer || renderer is TrailRenderer)
                        continue;

                    if (!found)
                    {
                        bounds = renderer.bounds;
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                if (!found)
                    throw new InvalidOperationException("Visual Root must contain the vehicle renderers.");

                _scaledPivot = Vector3.Scale(_visualRoot.InverseTransformPoint(bounds.center), _visualRoot.localScale);
                _timeline = new DefeatTimeline(_flipDuration, _explosionDelay, _minimumExplosionTime, _maximumDuration);
                _explosion.Initialize();
                _initialized = true;
            }

            _session = session ?? throw new ArgumentNullException(nameof(session));
            _session.StateChanged += OnStateChanged;

            ResetForRun();
            OnStateChanged(_session.State);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Ready)
                ResetForRun();

            else if (state == GameState.Lost && !_playing && !IsComplete)
            {
                _timeline.Reset();
                _playing = true;
            }
        }

        private void Update()
        {
            if (!_playing || Time.timeScale <= 0f)
                return;

            _explosion.Tick(Time.deltaTime);

            bool explode = _timeline.Step(Time.deltaTime, _explosion.IsAlive);
            float t = _timeline.FlipProgress;
            float smooth = t * t * (3f - 2f * t);
            Quaternion rotation = _restRotation * Quaternion.AngleAxis(_rollDegrees * smooth, Vector3.forward);
            Vector3 offset = new Vector3(_sideDistance * smooth, Mathf.Sin(t * Mathf.PI) * _hopHeight, 0f);
            _visualRoot.localRotation = rotation;
            _visualRoot.localPosition = _restPosition + _restRotation * _scaledPivot - rotation * _scaledPivot + offset;

            if (explode)
                _explosion.PlayAt(_explosionPoint.position, Quaternion.identity);

            if (_timeline.IsComplete)
                Finish();
        }

        private void Finish()
        {
            _playing = false;

            _explosion.StopAndClear();

            IsComplete = true;

            Completed?.Invoke();
        }

        public void ResetForRun()
        {
            _playing = IsComplete = false;
            _timeline?.Reset();

            if (_explosion != null)
                _explosion.StopAndClear();

            if (_initialized && _visualRoot != null)
            {
                _visualRoot.localPosition = _restPosition;
                _visualRoot.localRotation = _restRotation;
            }
        }

        public void Unbind()
        {
            if (_session != null)
                _session.StateChanged -= OnStateChanged;

            _session = null;

            ResetForRun();
        }

        private void OnDisable()
        {
            if (_playing)
                Finish();
        }

        private void OnDestroy() => Unbind();
    }
}