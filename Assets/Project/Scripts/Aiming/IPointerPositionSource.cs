using UnityEngine;

namespace SteelRide.Aiming
{
    public interface IPointerPositionSource
    {
        bool TryGetPosition(out Vector2 screenPosition);
    }
}