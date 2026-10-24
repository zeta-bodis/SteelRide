using System;
using SteelRide.Combat;

namespace SteelRide.Enemies
{
    public sealed class EnemyBrain
    {
        public const int VehicleContactDamage = 5;

        private bool _alerted;

        public EnemyState State { get; private set; } = EnemyState.Idle;
        public bool HasHitVehicle { get; private set; }

        public event Action<EnemyState> StateChanged;

        public void Refresh(bool canAct, bool isAlive, bool targetInRange)
        {
            if (State == EnemyState.Dead)
                return;

            if (!isAlive)
            {
                SetState(EnemyState.Dead);
                return;
            }

            if (canAct && targetInRange)
                _alerted = true;

            SetState(canAct && _alerted ? EnemyState.Chasing : EnemyState.Idle);
        }

        public bool TryHitVehicle(bool canAct, bool isAlive, IDamageable vehicle)
        {
            if (!canAct || !isAlive || State == EnemyState.Dead || HasHitVehicle || vehicle == null)
                return false;

            HasHitVehicle = true;

            if (vehicle.TryApplyDamage(VehicleContactDamage))
                return true;

            HasHitVehicle = false;

            return false;
        }

        private void SetState(EnemyState state)
        {
            if (State == state)
                return;

            State = state;

            StateChanged?.Invoke(state);
        }

        public void ResetForNewLife()
        {
            _alerted = false;

            HasHitVehicle = false;

            SetState(EnemyState.Idle);
        }
    }
}
