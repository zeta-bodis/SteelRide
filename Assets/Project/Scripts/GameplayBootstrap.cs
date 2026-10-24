using System;
using SteelRide.Aiming;
using SteelRide.CameraSystem;
using SteelRide.Core;
using SteelRide.Effects;
using SteelRide.Infrastructure;
using SteelRide.Input;
using SteelRide.Spawning;
using SteelRide.Turret;
using SteelRide.UI;
using SteelRide.Vehicle;
using SteelRide.Weapons;
using UnityEngine;

namespace SteelRide.Composition
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RunProgressTracker))]
    public sealed class GameplayBootstrap : MonoBehaviour, IRunResetter
    {
        [SerializeField] private PointerPressInput _input;

        [SerializeField] private VehicleMovement _vehicle;

        [SerializeField] private CameraFollow _cameraFollow;

        [SerializeField] private LegacyPointerAimInput _aimInput;

        [SerializeField] private GroundAimPointProvider _aimPointProvider;

        [SerializeField] private TurretAimController _turret;

        [SerializeField] private ProjectilePool _projectilePool;

        [SerializeField] private AutomaticWeapon _weapon;

        [SerializeField] private VehicleDefeatHandler _vehicleDefeat;

        [SerializeField] private LevelStreamer _levelStreamer;

        [SerializeField] private GameplayHud _hud;

        [SerializeField] private GameplayEffects _effects;

        private GameSession _session;
        private GameFlowController _flow;
        private GameTimeController _clock;
        private LevelProgress _levels;
        private RunProgressTracker _progress;
        private WheelVisualRotation _wheels;
        private Camera _camera;
        private float _startZ;
        private bool _started;

        private void Start()
        {
            if (_input == null || _vehicle == null || _cameraFollow == null || _aimInput == null || _aimPointProvider == null || _turret == null || _projectilePool == null || _weapon == null || _vehicleDefeat == null || _levelStreamer == null || _hud == null || _effects == null)
            {
                enabled = false;
                return;
            }

            _camera = _cameraFollow.GetComponent<Camera>();
            _progress = GetComponent<RunProgressTracker>();

            if (_vehicleDefeat.transform != _vehicle.transform || _camera == null || _progress == null)
            {
                enabled = false;
                return;
            }

            _session = new GameSession();
            _levels = new LevelProgress(new PlayerPrefsLevelStore());

            _startZ = _vehicle.transform.position.z;
            _wheels = _vehicle.GetComponentInChildren<WheelVisualRotation>(true);

            _cameraFollow.SetTarget(_vehicle.transform);
            _input.SetUiBlocker(_hud.PointerBlocker);
            _aimInput.SetUiBlocker(_hud.PointerBlocker);

            _started = true;

            BindRun();
        }

        private void OnEnable()
        {
            if (_started)
                BindRun();
        }

        private void BindRun()
        {
            try
            {
                _clock = new GameTimeController(_session);
                _vehicle.Bind(_session);
                _turret.Bind(_session, _aimInput, _aimPointProvider);
                _projectilePool.Bind(_session);
                _weapon.Bind(_session, _projectilePool, _vehicle.transform);
                _vehicleDefeat.Bind(_session);
                _levelStreamer.Bind(_session, _session, _vehicleDefeat.Health, _cameraFollow.transform);
                _effects.Bind(_session, _weapon, _projectilePool, _levelStreamer, _vehicle.transform, _turret.transform);
                _flow = new GameFlowController(_session, _input, this, _levels, _effects.Defeat);
                _progress.Bind(_session, _vehicle.transform, _startZ, _levelStreamer.FinishZ);
                _hud.Bind(_session, _flow, _vehicleDefeat.Health, _levels, _progress, _vehicle.transform, _camera, _effects.Defeat);
                _input.SuppressUntilRelease();
            }
            catch (Exception)
            {
                enabled = false;
                throw;
            }
        }

        public void ResetRun()
        {
            if (_session == null || (_session.State != GameState.Won && _session.State != GameState.Lost))
                throw new InvalidOperationException("World reset requires a finished run.");

            _input.SuppressUntilRelease();
            _aimInput.ResetPointer();
            _projectilePool.ResetForRun();
            _vehicle.ResetToStart();
            _vehicleDefeat.Health.RestoreFullHealth();
            _turret.ResetForRun();
            _wheels?.ResetForRun();
            _cameraFollow.SnapToTarget();

            Physics.SyncTransforms();

            _levelStreamer.ResetForRun();
            _effects.ResetForRun();
        }

        private void OnDisable()
        {
            _flow?.Dispose();
            _flow = null;

            if (_hud != null)
                _hud.Unbind();

            if (_effects != null)
                _effects.Unbind();

            if (_progress != null)
                _progress.Unbind();

            if (_session?.State == GameState.Paused)
                _session.TryResume();

            _clock?.Dispose();
            _clock = null;

            if (_levelStreamer != null)
                _levelStreamer.Unbind();

            if (_vehicleDefeat != null)
                _vehicleDefeat.Unbind();

            if (_weapon != null)
                _weapon.Unbind();

            if (_projectilePool != null)
                _projectilePool.Unbind();

            if (_vehicle != null)
                _vehicle.Unbind();

            if (_turret != null)
                _turret.Unbind();
        }
    }
}