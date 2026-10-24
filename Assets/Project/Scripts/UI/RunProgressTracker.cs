using System;
using SteelRide.Core;
using UnityEngine;

namespace SteelRide.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(400)]
    public sealed class RunProgressTracker : MonoBehaviour
    {
        [SerializeField, Min(0.02f)] private float _updateInterval = 0.1f;

        private Transform _vehicle;
        private IGameStateSource _session;
        private float _startZ, _finishZ, _nextSample;

        public RunDistance Current { get; private set; }

        public event Action<RunDistance> Changed;

        public void Bind(IGameStateSource session, Transform vehicle, float startZ, float finishZ)
        {
            Unbind();

            _session = session ?? throw new ArgumentNullException(nameof(session));
            _vehicle = vehicle != null ? vehicle : throw new ArgumentNullException(nameof(vehicle));
            _startZ = startZ;
            _finishZ = finishZ;
            _session.StateChanged += OnStateChanged;

            Sample();
        }

        public void Unbind()
        {
            if (_session != null)
                _session.StateChanged -= OnStateChanged;

            _session = null;
            _vehicle = null;
        }

        private void OnStateChanged(GameState state)
        {
            if (state != GameState.Paused)
                Sample();
        }

        public void Sample()
        {
            if (_vehicle == null)
                return;

            float position = _session.State == GameState.Won ? _finishZ : _vehicle.position.z;

            Current = new RunDistance(_startZ, _finishZ, position);

            _nextSample = Time.unscaledTime + _updateInterval;

            Changed?.Invoke(Current);
        }

        private void LateUpdate()
        {
            if (_session?.State == GameState.Playing && Time.unscaledTime >= _nextSample)
                Sample();
        }

        private void OnDestroy() => Unbind();
    }
}