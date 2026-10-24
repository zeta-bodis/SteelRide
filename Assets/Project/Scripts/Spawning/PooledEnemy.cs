using SteelRide.Core;
using SteelRide.Enemies;
using UnityEngine;

namespace SteelRide.Spawning
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class PooledEnemy : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _cleanupRadius = 3f;

        private EnemyController _enemy;
        private EnemyMovement _movement;
        private EnemyAnimationPresenter _animation;
        private EnemyContactKnockback _knockback;
        public float CleanupRadius => _cleanupRadius;
        public EnemyController Controller => _enemy;

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _movement = GetComponent<EnemyMovement>();
            _animation = GetComponent<EnemyAnimationPresenter>();
            _knockback = GetComponent<EnemyContactKnockback>();
        }

        public void Bind(IGameStateSource session, EnemyTargetContext target) => _enemy.Bind(session, target, true);

        public void Unbind()
        {
            if (_enemy != null)
                _enemy.Unbind();
        }

        public void Spawn(Vector3 position, Quaternion rotation)
        {
            _enemy.ResetForSpawn();

            if (_knockback != null)
                _knockback.ResetForSpawn();

            if (_animation != null)
                _animation.ResetForSpawn();

            _movement.Teleport(position, rotation);

            gameObject.SetActive(true);
        }

        public void Despawn()
        {
            if (_knockback != null)
                _knockback.CancelForPool();

            gameObject.SetActive(false);
        }
    }
}
