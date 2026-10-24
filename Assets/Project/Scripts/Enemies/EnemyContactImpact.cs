using UnityEngine;

namespace SteelRide.Enemies
{
    public readonly struct EnemyContactImpact
    {
        public Vector3 EnemyPosition { get; }
        public Bounds EnemyBounds { get; }
        public Bounds VehicleBounds { get; }
        public Vector3 VehicleRight { get; }
        public Vector3 ContactPosition { get; }
        public Vector3 ContactNormal { get; }

        public EnemyContactImpact(Vector3 enemyPosition, Bounds enemyBounds, Bounds vehicleBounds, Vector3 vehicleRight, Vector3 contactPosition, Vector3 contactNormal)
        {
            EnemyPosition = enemyPosition;
            EnemyBounds = enemyBounds;
            VehicleBounds = vehicleBounds;
            VehicleRight = vehicleRight;
            ContactPosition = contactPosition;
            ContactNormal = contactNormal;
        }
    }
}
