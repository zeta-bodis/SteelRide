using System;
using UnityEngine;

namespace SteelRide.Spawning
{
    [DisallowMultipleComponent]
    public sealed class GroundSegment : MonoBehaviour
    {
        [SerializeField] private Collider _surface;
        [SerializeField, Min(1f)] private float _length = 30f;
        [SerializeField, Min(1f)] private float _width = 12f;

        private Vector3 _boundsOffset, _boundsSize;
        public float Length => _length;
        public float Width => _width;
        public int SegmentIndex { get; private set; }
        public int NextEnemy { get; set; }
        public EnemySpawnLayout Layout { get; private set; }
        public Bounds VisualBounds => new Bounds(transform.position + _boundsOffset, _boundsSize);

        public void Initialize()
        {
            if (_surface == null || _surface.isTrigger || !_surface.enabled || !_surface.gameObject.activeInHierarchy || (_surface.transform != transform && !_surface.transform.IsChildOf(transform)))
                throw new InvalidOperationException("GroundSegment requires an enabled non-trigger Surface collider.");

            Bounds bounds = _surface.bounds;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            _boundsOffset = bounds.center - transform.position;
            _boundsSize = bounds.size;
        }

        public void Place(int index, Vector3 position, EnemySpawnLayout layout)
        {
            SegmentIndex = index;
            Layout = layout;
            NextEnemy = 0;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            gameObject.SetActive(true);
        }

        public bool TryFindSurface(Vector3 point, float probeHeight, out Vector3 position)
        {
            var ray = new Ray(point + Vector3.up * probeHeight, Vector3.down);

            if (_surface.Raycast(ray, out RaycastHit hit, probeHeight * 2f) && hit.normal.y >= 0.9f)
            {
                position = hit.point;
                return true;
            }

            position = default;
            return false;
        }
    }
}