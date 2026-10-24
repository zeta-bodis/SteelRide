using System;
using SteelRide.Combat;
using UnityEngine;

namespace SteelRide.Enemies
{
    public sealed class EnemyTargetContext
    {
        public HealthComponent Health { get; }
        public Rigidbody Body { get; }
        public Collider[] Colliders { get; }

        public EnemyTargetContext(HealthComponent health)
        {
            if (health == null)
                throw new ArgumentNullException(nameof(health));

            Health = health;
            Body = health.GetComponent<Rigidbody>();
            Colliders = health.GetComponentsInChildren<Collider>(true);
        }
    }
}
