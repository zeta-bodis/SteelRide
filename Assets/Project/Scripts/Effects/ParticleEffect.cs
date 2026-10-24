using System;
using UnityEngine;

namespace SteelRide.Effects
{
    [DisallowMultipleComponent]
    public sealed class ParticleEffect : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _maximumDuration = 6f;

        private ParticleSystem[] _systems;
        private float _elapsed;
        private bool _playing;

        public bool IsAlive
        {
            get
            {
                if (!_playing || !gameObject.activeInHierarchy)
                    return false;

                for (int i = 0; i < _systems.Length; i++)
                    if (_systems[i].IsAlive(false))
                        return true;

                return false;
            }
        }

        public void Initialize()
        {
            if (_systems != null)
                return;

            _systems = GetComponentsInChildren<ParticleSystem>(true);

            if (_systems.Length == 0)
                throw new InvalidOperationException("ParticleEffect needs at least one ParticleSystem.");

            for (int i = 0; i < _systems.Length; i++)
            {
                ParticleSystem ps = _systems[i];
                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.playOnAwake = false;
                main.loop = false;
                main.useUnscaledTime = false;
                main.stopAction = ParticleSystemStopAction.None;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

                if (ps.gameObject.activeInHierarchy)
                    ps.Simulate(0.01f, false, true, true);

                ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public void PlayAt(Vector3 position, Quaternion rotation)
        {
            Initialize();
            StopAndClear();

            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);

            _elapsed = 0f;
            _playing = true;

            for (int i = 0; i < _systems.Length; i++)
                if (_systems[i].gameObject.activeInHierarchy)
                    _systems[i].Play(false);
        }

        public void Tick(float deltaTime)
        {
            if (!_playing || deltaTime <= 0f)
                return;

            _elapsed += deltaTime;

            if (_elapsed >= _maximumDuration)
                StopAndClear();
        }

        public void StopAndClear()
        {
            _playing = false;
            _elapsed = 0f;

            if (_systems == null)
                return;

            for (int i = 0; i < _systems.Length; i++)
                if (_systems[i] != null)
                    _systems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDisable() => StopAndClear();
    }
}