namespace SteelRide.Combat
{
    public interface IDamageable
    {
        bool TryApplyDamage(int amount);
    }
}