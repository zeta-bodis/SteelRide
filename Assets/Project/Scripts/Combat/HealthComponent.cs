using System;
using UnityEngine;

namespace SteelRide.Combat
{
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int _maxHealth = 100;

        private Health _health;

        public event Action<int, int> Changed;
        public event Action Died;
        public int CurrentHealth => Model.Current;
        public int MaxHealth => Model.Maximum;
        public bool IsAlive => Model.IsAlive;

        private Health Model
        {
            get
            {
                if (_health == null)
                {
                    _health = new Health(Mathf.Max(1, _maxHealth));
                    _health.Changed += OnHealthChanged;
                    _health.Died += OnDied;
                }

                return _health;
            }
        }

        private void Awake()
        {
            _ = Model;
        }

        public bool TryApplyDamage(int amount)
        {
            return isActiveAndEnabled && Model.TryApplyDamage(amount);
        }

        public void ResetForSpawn()
        {
            if (gameObject.activeInHierarchy)
                throw new InvalidOperationException("Reset health only while the pooled actor is inactive.");

            Model.ResetForNewLife();
        }

        public void RestoreFullHealth() => Model.ResetForNewLife();

        private void OnHealthChanged(int current, int maximum) => Changed?.Invoke(current, maximum);

        private void OnDied() => Died?.Invoke();

        private void OnDestroy()
        {
            if (_health == null)
                return;

            _health.Changed -= OnHealthChanged;
            _health.Died -= OnDied;
        }

        private void OnValidate() => _maxHealth = Mathf.Max(1, _maxHealth);
    }
}