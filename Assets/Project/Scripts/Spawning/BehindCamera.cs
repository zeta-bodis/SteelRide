using System;
using UnityEngine;

namespace SteelRide.Spawning
{
    public static class BehindCamera
    {
        public static bool ContainsEntireBounds(Bounds bounds, Vector3 cameraPosition, Vector3 cameraForward, float margin)
        {
            Vector3 extent = bounds.extents;
            float radius = Math.Abs(cameraForward.x) * extent.x + Math.Abs(cameraForward.y) * extent.y + Math.Abs(cameraForward.z) * extent.z;
            return Vector3.Dot(bounds.center - cameraPosition, cameraForward) + radius < -margin;
        }

        public static bool ContainsEntireSphere(Vector3 center, float radius, Vector3 cameraPosition, Vector3 cameraForward, float margin)
        {
            return Vector3.Dot(center - cameraPosition, cameraForward) + radius < -margin;
        }
    }
}
