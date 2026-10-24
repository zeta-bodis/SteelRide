namespace SteelRide.Weapons
{
    public interface IProjectileSpawner
    {
        bool TrySpawn(ProjectileSpawnData data);
    }
}