using UnityEngine;

namespace SteelRide.Aiming
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class GroundAimPointProvider : MonoBehaviour, IAimPointProvider
    {
        [SerializeField] private LayerMask _groundMask;
        [SerializeField, Min(1f)] private float _maxDistance = 500f;

        private Camera _camera;

        private void Reset()
        {
            _groundMask = LayerMask.GetMask("Ground");
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (_groundMask.value == 0)
                enabled = false;
        }

        private void OnValidate()
        {
            _maxDistance = Mathf.Max(1f, _maxDistance);
        }

        public bool TryGetAimPoint(Vector2 screenPosition, out Vector3 worldPoint)
        {
            worldPoint = default;

            if (!isActiveAndEnabled || _camera == null || !_camera.isActiveAndEnabled || !_camera.pixelRect.Contains(screenPosition))
                return false;

            Ray ray = _camera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _groundMask, QueryTriggerInteraction.Ignore))
                return false;

            worldPoint = hit.point;
            return true;
        }
    }
}