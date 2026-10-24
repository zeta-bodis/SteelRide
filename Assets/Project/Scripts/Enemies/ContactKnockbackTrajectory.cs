using System;
using UnityEngine;

namespace SteelRide.Enemies
{
    public readonly struct ContactKnockbackTrajectory
    {
        public Vector3 Start { get; }
        public Vector3 End { get; }

        private readonly float _height;

        private ContactKnockbackTrajectory(Vector3 start, Vector3 end, float height)
        {
            Start = start;
            End = end;
            _height = height;
        }

        public static ContactKnockbackTrajectory Create(EnemyContactImpact impact, float extraDistance, float clearance, float height)
        {
            ValidateNonNegative(extraDistance, nameof(extraDistance));
            ValidateNonNegative(clearance, nameof(clearance));
            ValidateNonNegative(height, nameof(height));
            Vector3 right = impact.VehicleRight;
            right.y = 0f;
            float lengthSquared = right.sqrMagnitude;
            right = lengthSquared > 0.0001f ? right / (float)Math.Sqrt(lengthSquared) : Vector3.right;
            float side = Vector3.Dot(impact.EnemyBounds.center - impact.VehicleBounds.center, right);
            Vector3 direction = side < 0f ? -right : right;
            float vehicleEdge = Vector3.Dot(impact.VehicleBounds.center, direction) + HorizontalExtent(impact.VehicleBounds, direction);
            float enemyNearEdge = Vector3.Dot(impact.EnemyBounds.center, direction) - HorizontalExtent(impact.EnemyBounds, direction);
            float separation = Math.Max(0f, vehicleEdge + clearance - enemyNearEdge);
            Vector3 end = impact.EnemyPosition + direction * (separation + extraDistance);
            return new ContactKnockbackTrajectory(impact.EnemyPosition, end, height);
        }

        public Vector3 PositionAt(float progress)
        {
            if (float.IsNaN(progress) || float.IsInfinity(progress))
                throw new ArgumentOutOfRangeException(nameof(progress));

            float t = Math.Max(0f, Math.Min(1f, progress));
            float remaining = 1f - t;
            float horizontalProgress = 1f - remaining * remaining * remaining;
            Vector3 position = Start + (End - Start) * horizontalProgress;
            position.y = Start.y + 4f * _height * t * remaining;
            return position;
        }

        private static float HorizontalExtent(Bounds bounds, Vector3 direction)
        {
            return Math.Abs(direction.x) * bounds.extents.x + Math.Abs(direction.z) * bounds.extents.z;
        }

        private static void ValidateNonNegative(float value, string name)
        {
            if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name);
        }
    }
}