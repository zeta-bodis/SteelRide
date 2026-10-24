using System;
using SteelRide.Spawning;
using UnityEngine;

namespace SteelRide.Effects
{
    [DisallowMultipleComponent]
    public sealed class ParticleEffectPool : MonoBehaviour
    {
        [SerializeField] private ParticleEffect _prefab;
        [SerializeField, Min(1)] private int _capacity = 16;

        private FixedPool<ParticleEffect> _pool;
        private Transform _staging;

        public void Initialize()
        {
            if (_pool != null)
                return;

            if (_prefab == null || _capacity < 1)
                throw new InvalidOperationException("ParticleEffectPool: assign prefab and positive capacity.");

            _staging = new GameObject("Inactive VFX staging").transform;
            _staging.SetParent(transform, false);
            _staging.gameObject.SetActive(false);
            _pool = new FixedPool<ParticleEffect>(_capacity, Create);
        }

        private ParticleEffect Create(int index)
        {
            ParticleEffect effect = Instantiate(_prefab, _staging);
            effect.gameObject.SetActive(false);
            effect.transform.SetParent(transform, false);
            effect.gameObject.SetActive(true);
            effect.Initialize();
            effect.gameObject.SetActive(false);
            return effect;
        }

        public bool TryPlay(Vector3 position, Vector3 normal)
        {
            if (!isActiveAndEnabled || _pool == null)
                return false;

            if (!_pool.TryRent(out ParticleEffect effect, out _))
                return false;

            Quaternion rotation = normal.sqrMagnitude > 0.0001f ? Quaternion.FromToRotation(Vector3.forward, normal.normalized) : Quaternion.identity;
            effect.PlayAt(position, rotation);
            return true;
        }

        private void LateUpdate()
        {
            if (_pool == null || Time.timeScale <= 0f)
                return;

            for (int i = 0; i < _pool.Capacity; i++)
            {
                if (!_pool.IsRented(i))
                    continue;

                ParticleEffect effect = _pool.ItemAt(i);
                effect.Tick(Time.deltaTime);

                if (effect.IsAlive)
                    continue;

                ReturnToPool(i);
            }
        }

        public void Clear()
        {
            if (_pool == null)
                return;

            for (int i = 0; i < _pool.Capacity; i++)
            {
                if (!_pool.IsRented(i))
                    continue;

                ReturnToPool(i);
            }
        }

        private void ReturnToPool(int index)
        {
            ParticleEffect effect = _pool.ItemAt(index);
            effect.StopAndClear();
            effect.gameObject.SetActive(false);
            _pool.TryReturn(index);
        }

        private void OnDisable() => Clear();
    }
}