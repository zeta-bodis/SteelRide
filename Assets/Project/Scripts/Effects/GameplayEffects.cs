using System;
using SteelRide.Core;
using SteelRide.Enemies;
using SteelRide.Spawning;
using SteelRide.Weapons;
using UnityEngine;

namespace SteelRide.Effects
{
    [DisallowMultipleComponent]
    public sealed class GameplayEffects : MonoBehaviour
    {
        [SerializeField] private VehicleDefeatSequence _defeat;
        [SerializeField] private ParticleEffect _muzzleFlash;
        [SerializeField] private ParticleSystem _exhaust;
        [SerializeField] private ParticleEffectPool _enemyHits;
        [SerializeField] private ParticleEffectPool _vehicleContacts;

        private IGameStateSource _session;
        private AutomaticWeapon _weapon;
        private ProjectilePool _projectiles;
        private LevelStreamer _streamer;

        public IDefeatPresentation Defeat => _defeat;

        public void Bind(IGameStateSource session, AutomaticWeapon weapon, ProjectilePool projectiles, LevelStreamer streamer, Transform vehicle, Transform turret)
        {
            Unbind();

            if (!isActiveAndEnabled || _defeat == null || _muzzleFlash == null || _exhaust == null || _enemyHits == null || _vehicleContacts == null)
                throw new InvalidOperationException("GameplayEffects: assign all five effect references.");

            _enemyHits.Initialize();
            _vehicleContacts.Initialize();
            _muzzleFlash.Initialize();
            _exhaust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _exhaust.main;
            main.useUnscaledTime = false;
            main.playOnAwake = false;
            main.loop = true;
            main.stopAction = ParticleSystemStopAction.None;

            _defeat.Bind(session, vehicle, turret);
            _session = session;
            _weapon = weapon;
            _projectiles = projectiles;
            _streamer = streamer;
            _weapon.ShotFired += OnShot;
            _projectiles.Impact += OnImpact;
            _streamer.EnemyContact += OnContact;
            _session.StateChanged += OnStateChanged;

            OnStateChanged(_session.State);
        }

        private void OnShot() => _muzzleFlash.PlayAt(_muzzleFlash.transform.position, _muzzleFlash.transform.rotation);

        private void OnImpact(ProjectileImpact impact)
        {
            if (impact.DamageApplied && impact.Collider != null && impact.Collider.GetComponentInParent<EnemyController>() != null)
                _enemyHits.TryPlay(impact.Position, impact.Normal);
        }

        private void OnContact(EnemyContactImpact impact) => _vehicleContacts.TryPlay(impact.ContactPosition, impact.ContactNormal);

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Paused)
                return;

            if (state == GameState.Ready)
            {
                ResetForRun();

                _exhaust.Play(true);
            }
            else if (state == GameState.Playing)
            {
                if (!_exhaust.isPlaying)
                    _exhaust.Play(true);
            }
            else
            {
                _exhaust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                _muzzleFlash.StopAndClear();
            }
        }

        private void LateUpdate()
        {
            if (_session != null && Time.timeScale > 0f)
                _muzzleFlash.Tick(Time.deltaTime);
        }

        public void ResetForRun()
        {
            if (_defeat != null)
                _defeat.ResetForRun();

            if (_muzzleFlash != null)
                _muzzleFlash.StopAndClear();

            if (_exhaust != null)
                _exhaust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_enemyHits != null)
                _enemyHits.Clear();

            if (_vehicleContacts != null)
                _vehicleContacts.Clear();
        }

        public void Unbind()
        {
            if (_weapon != null)
                _weapon.ShotFired -= OnShot;

            if (_projectiles != null)
                _projectiles.Impact -= OnImpact;

            if (_streamer != null)
                _streamer.EnemyContact -= OnContact;

            if (_session != null)
                _session.StateChanged -= OnStateChanged;

            if (_defeat != null)
                _defeat.Unbind();

            _session = null;
            _weapon = null;
            _projectiles = null;
            _streamer = null;

            ResetForRun();
        }

        private void OnDisable() => Unbind();
    }
}