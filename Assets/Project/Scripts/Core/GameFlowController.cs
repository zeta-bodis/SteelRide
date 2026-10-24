using System;

namespace SteelRide.Core
{
    public sealed class GameFlowController : IGameCommands, IDisposable
    {
        private readonly GameSession _session;
        private readonly IPressInput _input;
        private readonly IRunResetter _resetter;
        private readonly LevelProgress _levels;
        private readonly IDefeatPresentation _defeat;
        private bool _resetting;
        private bool _disposed;

        public GameFlowController(GameSession session, IPressInput input, IRunResetter resetter, LevelProgress levels, IDefeatPresentation defeat = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _resetter = resetter ?? throw new ArgumentNullException(nameof(resetter));
            _levels = levels ?? throw new ArgumentNullException(nameof(levels));
            _defeat = defeat;
            _input.Pressed += OnPressed;
            _session.StateChanged += OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Won)
                _levels.Advance();
        }

        private void OnPressed()
        {
            if (!_disposed && !_resetting)
                _session.TryStart();
        }

        public void TogglePause()
        {
            if (_disposed || _resetting)
                return;

            if (_session.State == GameState.Paused)
                _session.TryResume();
            else
                _session.TryPause();
        }

        public void Restart()
        {
            if (_disposed || _resetting || (_session.State != GameState.Won && _session.State != GameState.Lost) || (_session.State == GameState.Lost && _defeat != null && !_defeat.IsComplete))
                return;

            _resetting = true;

            try
            {
                _resetter.ResetRun();
                _session.TryReset();
            }
            finally
            {
                _resetting = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _input.Pressed -= OnPressed;
            _session.StateChanged -= OnStateChanged;
        }
    }
}