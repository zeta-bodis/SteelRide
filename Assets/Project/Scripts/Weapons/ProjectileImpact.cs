using UnityEngine;

namespace SteelRide.Weapons
{
    public readonly struct ProjectileImpact
    {
        public Collider Collider { get; }
        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public bool DamageApplied { get; }

        public ProjectileImpact(Collider collider, Vector3 position, Vector3 normal, bool damageApplied)
        {
            Collider = collider;
            Position = position;
            Normal = normal;
            DamageApplied = damageApplied;
        }
    }
}