using System;
using TMPro;
using SteelRide.Combat;
using SteelRide.Core;
using SteelRide.Input;
using UnityEngine;
using UnityEngine.UI;

namespace SteelRide.UI
{
    public enum DistanceDisplay
    {
        Remaining,
        Travelled
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(UiPointerBlocker))]
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private GameObject _readyHint;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TMP_Text _resultTitle;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private TMP_Text _pauseLabel;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private WorldHealthBar _healthBar;
        [SerializeField] private Slider _distanceSlider;
        [SerializeField] private TMP_Text _totalDistance;
        [SerializeField] private TMP_Text _distanceMeters;
        [SerializeField] private TMP_Text _distanceCaption;
        [SerializeField] private DistanceDisplay _distanceDisplay = DistanceDisplay.Remaining;

        private IGameStateSource _session;
        private IGameCommands _commands;
        private HealthComponent _health;
        private LevelProgress _levels;
        private RunProgressTracker _progress;
        private IDefeatPresentation _defeat;
        private int _lastMeters = -1, _lastTotal = -1;

        public UiPointerBlocker PointerBlocker => GetComponent<UiPointerBlocker>();

        public void Bind(IGameStateSource session, IGameCommands commands, HealthComponent health, LevelProgress levels, RunProgressTracker progress, Transform vehicle, Camera camera, IDefeatPresentation defeat = null)
        {
            Unbind();

            if (_readyHint == null || _resultPanel == null || _resultTitle == null || _restartButton == null || _pauseButton == null || _pauseLabel == null || _levelLabel == null || _healthSlider == null || _healthBar == null || _distanceSlider == null || _totalDistance == null || _distanceMeters == null || _distanceCaption == null || camera == null)
                throw new InvalidOperationException("GameplayHud: assign every UI reference and the camera.");

            _session = session;
            _commands = commands;
            _health = health;
            _levels = levels;
            _progress = progress;
            _defeat = defeat;

            if (_defeat != null)
                _defeat.Completed += OnDefeatComplete;

            _lastMeters = _lastTotal = -1;

            ConfigureSlider(_healthSlider);
            ConfigureSlider(_distanceSlider);

            _healthBar.Bind(vehicle, camera);
            _restartButton.onClick.AddListener(Restart);
            _pauseButton.onClick.AddListener(TogglePause);
            _session.StateChanged += OnStateChanged;
            _health.Changed += OnHealthChanged;
            _levels.Changed += OnLevelChanged;
            _progress.Changed += OnDistanceChanged;

            OnHealthChanged(_health.CurrentHealth, _health.MaxHealth);
            OnLevelChanged(_levels.Current);
            OnDistanceChanged(_progress.Current);
            OnStateChanged(_session.State);
        }

        public void Unbind()
        {
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(Restart);

            if (_pauseButton != null)
                _pauseButton.onClick.RemoveListener(TogglePause);

            if (_session != null)
                _session.StateChanged -= OnStateChanged;

            if (_health != null)
                _health.Changed -= OnHealthChanged;

            if (_levels != null)
                _levels.Changed -= OnLevelChanged;

            if (_progress != null)
                _progress.Changed -= OnDistanceChanged;

            if (_defeat != null)
                _defeat.Completed -= OnDefeatComplete;

            _defeat = null;

            if (_healthBar != null)
                _healthBar.Unbind();

            _session = null;
            _commands = null;
            _health = null;
            _levels = null;
            _progress = null;
        }

        private void Restart() => _commands?.Restart();

        private static void ConfigureSlider(Slider slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.interactable = false;
        }

        private void TogglePause() => _commands?.TogglePause();

        private void OnDefeatComplete()
        {
            if (_session?.State == GameState.Lost)
                OnStateChanged(GameState.Lost);
        }

        private void OnStateChanged(GameState state)
        {
            bool terminal = state == GameState.Won || state == GameState.Lost;
            _readyHint.SetActive(state == GameState.Ready);

            bool showResult = state == GameState.Won || (state == GameState.Lost && (_defeat == null || _defeat.IsComplete));
            _resultPanel.SetActive(showResult);

            _resultTitle.text = state == GameState.Won ? "You win" : "You lose";
            _pauseButton.interactable = state == GameState.Playing || state == GameState.Paused;
            _pauseLabel.text = state == GameState.Paused ? "Resume" : "Pause";
            _healthBar.SetVisible(!terminal);
        }

        private void OnHealthChanged(int current, int maximum) => _healthSlider.SetValueWithoutNotify((float)current / maximum);

        private void OnLevelChanged(int level)
        {
            _levelLabel.text = "Level " + level;
        }

        private void OnDistanceChanged(RunDistance distance)
        {
            _distanceSlider.SetValueWithoutNotify(distance.Normalized);

            int total = Mathf.CeilToInt(distance.Total);
            int meters = _distanceDisplay == DistanceDisplay.Remaining ? Mathf.CeilToInt(distance.Remaining) : Mathf.FloorToInt(distance.Travelled);

            if (total != _lastTotal)
            {
                _lastTotal = total;
                _totalDistance.SetText("{0:0}m", total);
            }

            if (meters != _lastMeters)
            {
                _lastMeters = meters;
                _distanceMeters.SetText("({0:0}m)", meters);
            }

            _distanceCaption.text = _distanceDisplay == DistanceDisplay.Remaining ? "Remaining" : "Travelled";
        }

        private void OnDestroy() => Unbind();
    }
}