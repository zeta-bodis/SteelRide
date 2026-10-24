using System;
using SteelRide.Combat;
using UnityEngine;

namespace SteelRide.Weapons
{
    [DisallowMultipleComponent]
    public sealed class Projectile : MonoBehaviour
    {
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
        private readonly Collider[] _overlapBuffer = new Collider[16];
        private ProjectileSpawnData _data;
        private int _hitMask;
        private float _remainingLifetime;
        private bool _launched;
        private bool _paused;
        private Action<Projectile> _returnToPool;
        private Action<ProjectileImpact> _reportImpact;

        private void Awake()
        {
            foreach (Collider projectileCollider in GetComponentsInChildren<Collider>(true))
                projectileCollider.enabled = false;
        }

        internal void Launch(ProjectileSpawnData data, int hitMask, Action<Projectile> returnToPool, Action<ProjectileImpact> reportImpact)
        {
            _data = data;
            _hitMask = hitMask;
            _remainingLifetime = data.Lifetime;
            _returnToPool = returnToPool;
            _reportImpact = reportImpact;

            transform.SetPositionAndRotation(data.Position, Quaternion.LookRotation(data.Direction));

            _launched = true;
            _paused = false;

            gameObject.SetActive(true);
        }

        internal void Despawn()
        {
            _launched = false;
            _remainingLifetime = 0f;
            _data = default;
            _returnToPool = null;
            _reportImpact = null;

            gameObject.SetActive(false);
        }

        internal void SetPaused(bool paused) => _paused = paused;

        private void Update() => Simulate(Time.deltaTime);

        internal void Simulate(float deltaTime)
        {
            if (!_launched || _paused || deltaTime <= 0f)
                return;

            float stepTime = Mathf.Min(deltaTime, _remainingLifetime);
            float distance = _data.Speed * stepTime;
            Vector3 origin = transform.position;

            if (TryFindOverlap(origin, out Collider overlap))
            {
                ResolveHit(overlap, overlap.ClosestPoint(origin), -_data.Direction);
                return;
            }

            if (TryFindSweepHit(origin, distance, out RaycastHit hit))
            {
                ResolveHit(hit.collider, hit.point, hit.normal);
                return;
            }

            transform.position = origin + _data.Direction * distance;
            _remainingLifetime -= stepTime;

            if (_remainingLifetime <= 0f)
                Return();
        }

        private bool TryFindOverlap(Vector3 origin, out Collider nearest)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, _data.Radius, _overlapBuffer, _hitMask, QueryTriggerInteraction.Ignore);

            Collider[] overlaps = _overlapBuffer;

            if (count == overlaps.Length)
            {
                overlaps = Physics.OverlapSphere(origin, _data.Radius, _hitMask, QueryTriggerInteraction.Ignore);
                count = overlaps.Length;
            }

            nearest = null;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                Collider candidate = overlaps[i];

                if (!CanHit(candidate))
                    continue;

                float distance = (candidate.ClosestPoint(origin) - origin).sqrMagnitude;

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearest = candidate;
            }

            return nearest != null;
        }

        private bool TryFindSweepHit(Vector3 origin, float distance, out RaycastHit nearest)
        {
            int count = Physics.SphereCastNonAlloc(origin, _data.Radius, _data.Direction, _hitBuffer, distance, _hitMask, QueryTriggerInteraction.Ignore);
            RaycastHit[] hits = _hitBuffer;

            if (count == hits.Length)
            {
                hits = Physics.SphereCastAll(origin, _data.Radius, _data.Direction, distance, _hitMask, QueryTriggerInteraction.Ignore);
                count = hits.Length;
            }

            nearest = default;
            float nearestDistance = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                if (!CanHit(hits[i].collider) || hits[i].distance >= nearestDistance)
                    continue;

                nearestDistance = hits[i].distance;
                nearest = hits[i];
                found = true;
            }

            return found;
        }

        private bool CanHit(Collider candidate)
        {
            if (candidate == null || candidate.isTrigger)
                return false;

            Transform hitTransform = candidate.transform;

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                return false;

            Transform owner = _data.Owner;
            return owner == null || (hitTransform != owner && !hitTransform.IsChildOf(owner));
        }

        private void ResolveHit(Collider collider, Vector3 position, Vector3 normal)
        {
            ProjectileSpawnData data = _data;
            IDamageable damageable = collider.GetComponentInParent<IDamageable>();
            Action<ProjectileImpact> report = _reportImpact;

            Return();

            bool applied = damageable != null && damageable.TryApplyDamage(data.Damage);
            report?.Invoke(new ProjectileImpact(collider, position, normal, applied));
        }

        private void Return()
        {
            if (!_launched)
                return;

            _launched = false;
            _returnToPool?.Invoke(this);
        }

        private void OnDisable() => Return();
    }
}