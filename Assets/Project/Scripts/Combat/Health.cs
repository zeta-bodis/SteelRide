using System;

namespace SteelRide.Combat
{
    public sealed class Health : IDamageable
    {
        public int Maximum { get; }
        public int Current { get; private set; }
        public bool IsAlive => Current > 0;

        public event Action<int, int> Changed;
        public event Action Died;

        public Health(int maximum)
        {
            if (maximum <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximum));

            Maximum = maximum;
            Current = maximum;
        }

        public bool TryApplyDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return false;

            Current -= Math.Min(Current, amount);

            bool diedFromThisHit = Current == 0;

            Changed?.Invoke(Current, Maximum);

            if (diedFromThisHit)
                Died?.Invoke();

            return true;
        }

        public void ResetForNewLife()
        {
            Current = Maximum;
            Changed?.Invoke(Current, Maximum);
        }
    }
}