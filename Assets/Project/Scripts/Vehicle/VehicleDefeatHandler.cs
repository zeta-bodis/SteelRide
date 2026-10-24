using System;
using SteelRide.Combat;
using SteelRide.Core;
using UnityEngine;

namespace SteelRide.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class VehicleDefeatHandler : MonoBehaviour
    {
        private HealthComponent _health;
        private IGameOutcome _outcome;
        private bool _subscribed;
        public HealthComponent Health
        {
            get
            {
                if (_health == null)
                    _health = GetComponent<HealthComponent>();

                return _health;
            }
        }

        public void Bind(IGameOutcome outcome)
        {
            if (outcome == null)
                throw new ArgumentNullException(nameof(outcome));

            Unbind();

            _outcome = outcome;

            Subscribe();
        }

        public void Unbind()
        {
            Unsubscribe();

            _outcome = null;
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unbind();

        private void Subscribe()
        {
            if (_subscribed || _outcome == null || !isActiveAndEnabled)
                return;

            Health.Died += OnDied;
            _subscribed = true;

            if (!Health.IsAlive)
                OnDied();
        }

        private void Unsubscribe()
        {
            if (_subscribed && _health != null)
                _health.Died -= OnDied;

            _subscribed = false;
        }

        private void OnDied() => _outcome?.TryLose();
    }
}