using UnityEngine;

namespace SteelRide.Aiming
{
    public interface IAimPointProvider
    {
        bool TryGetAimPoint(Vector2 screenPosition, out Vector3 worldPoint);
    }
}