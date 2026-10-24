using UnityEngine;

namespace SteelRide.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class EnemyMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _speed = 8f;
        [SerializeField, Min(0f)] private float _turnSpeed = 540f;

        private Rigidbody _body;

        public Vector3 Position
        {
            get
            {
                EnsureBody();

                return _body.position;
            }
        }

        private void Awake() => EnsureBody();

        private void EnsureBody()
        {
            if (_body != null)
                return;

            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void MoveTowards(Vector3 target, float deltaTime)
        {
            if (!isActiveAndEnabled || deltaTime <= 0f)
                return;

            EnsureBody();

            Vector3 direction = target - _body.position;
            direction.y = 0f;
            float distance = direction.magnitude;

            if (distance < 0.001f)
                return;

            direction /= distance;
            _body.MovePosition(_body.position + direction * Mathf.Min(_speed * deltaTime, distance));
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            _body.MoveRotation(Quaternion.RotateTowards(_body.rotation, rotation, _turnSpeed * deltaTime));
        }

        public void Stop()
        {
            EnsureBody();

            _body.MovePosition(_body.position);
            _body.MoveRotation(_body.rotation);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            EnsureBody();

            _body.position = position;
            _body.rotation = rotation;

            transform.SetPositionAndRotation(position, rotation);
        }

        private void OnDisable() => Stop();

        private void OnValidate()
        {
            _speed = Mathf.Max(0f, _speed);
            _turnSpeed = Mathf.Max(0f, _turnSpeed);
        }
    }
}
