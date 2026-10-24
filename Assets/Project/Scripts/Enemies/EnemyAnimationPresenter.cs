using System;
using UnityEngine;

namespace SteelRide.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyAnimationPresenter : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _idleState = "Base Layer.Idle";
        [SerializeField] private string _runState = "Base Layer.Run";
        [SerializeField] private string _deathState = "Base Layer.Death";
        [SerializeField, Min(0f)] private float _blendDuration = 0.1f;

        private EnemyController _enemy;
        private int _idleHash;
        private int _runHash;
        private int _deathHash;
        private bool _ready;
        private Action<EnemyState> _stateHandler;
        private Vector3 _modelPosition;
        private Quaternion _modelRotation;
        private Vector3 _modelScale;

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _stateHandler = OnStateChanged;

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                enabled = false;
                return;
            }

            _idleHash = Animator.StringToHash(_idleState);
            _runHash = Animator.StringToHash(_runState);
            _deathHash = Animator.StringToHash(_deathState);

            if (!_animator.HasState(0, _idleHash) || !_animator.HasState(0, _runHash) || !_animator.HasState(0, _deathHash))
            {
                enabled = false;
                return;
            }

            _animator.applyRootMotion = false;
            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _modelPosition = _animator.transform.localPosition;
            _modelRotation = _animator.transform.localRotation;
            _modelScale = _animator.transform.localScale;
            _ready = true;
        }

        private void OnEnable()
        {
            if (!_ready)
                return;

            _enemy.StateChanged += _stateHandler;

            Play(_enemy.State, true);
        }

        private void OnDisable()
        {
            if (_enemy != null)
                _enemy.StateChanged -= _stateHandler;
        }

        private void OnStateChanged(EnemyState state) => Play(state, false);

        public void ResetForSpawn()
        {
            if (!_ready || _animator.transform == transform)
                return;

            _animator.transform.localPosition = _modelPosition;
            _animator.transform.localRotation = _modelRotation;
            _animator.transform.localScale = _modelScale;
        }

        private void Play(EnemyState state, bool immediate)
        {
            if (!_ready || !isActiveAndEnabled || _animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;

            int hash = state == EnemyState.Dead ? _deathHash : state == EnemyState.Chasing ? _runHash : _idleHash;

            if (immediate || state == EnemyState.Dead || _blendDuration <= 0f)
                _animator.Play(hash, 0, 0f);
            else
                _animator.CrossFadeInFixedTime(hash, _blendDuration, 0, 0f);

            if (immediate)
                _animator.Update(0f);
        }
    }
}