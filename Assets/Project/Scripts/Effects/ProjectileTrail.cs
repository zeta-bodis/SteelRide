using UnityEngine;

namespace SteelRide.Effects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class ProjectileTrail : MonoBehaviour
    {
        private TrailRenderer _trail;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
            _trail.autodestruct = false;
        }

        private void OnEnable()
        {
            _trail.Clear();
            _trail.emitting = true;
        }

        private void OnDisable()
        {
            if (_trail == null)
                return;

            _trail.emitting = false;
            _trail.Clear();
        }
    }
}