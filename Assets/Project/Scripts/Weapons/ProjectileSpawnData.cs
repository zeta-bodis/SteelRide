using System;
using UnityEngine;

namespace SteelRide.Weapons
{
    public readonly struct ProjectileSpawnData
    {
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public Transform Owner { get; }
        public int Damage { get; }
        public float Speed { get; }
        public float Lifetime { get; }
        public float Radius { get; }

        public ProjectileSpawnData(Vector3 position, Vector3 direction, Transform owner, int damage, float speed, float lifetime, float radius)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (damage <= 0)
                throw new ArgumentOutOfRangeException(nameof(damage));

            RequirePositive(speed, nameof(speed));
            RequirePositive(lifetime, nameof(lifetime));
            RequirePositive(radius, nameof(radius));

            if (!IsFinite(position) || !IsFinite(direction) || direction.sqrMagnitude < 0.0001f)
                throw new ArgumentException("Projectile position and direction must be finite; direction cannot be zero.");

            Position = position;
            Direction = direction.normalized;
            Owner = owner;
            Damage = damage;
            Speed = speed;
            Lifetime = lifetime;
            Radius = radius;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void RequirePositive(float value, string name)
        {
            if (!IsFinite(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}