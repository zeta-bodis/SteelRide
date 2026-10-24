using System;
using SteelRide.Core;
using UnityEngine;

namespace SteelRide.Infrastructure
{
    public sealed class GameTimeController : IDisposable
    {
        private readonly IGameStateSource _session;
        private bool _paused;
        private float _previousScale;
        private bool _previousAudioPause;

        public GameTimeController(IGameStateSource session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _session.StateChanged += OnStateChanged;

            OnStateChanged(_session.State);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Paused && !_paused)
            {
                _previousScale = Time.timeScale;
                _previousAudioPause = AudioListener.pause;
                _paused = true;

                Time.timeScale = 0f;
                AudioListener.pause = true;
            }
            else if (state != GameState.Paused)
            {
                Restore();
            }
        }

        private void Restore()
        {
            if (!_paused)
                return;

            Time.timeScale = _previousScale;
            AudioListener.pause = _previousAudioPause;

            _paused = false;
        }

        public void Dispose()
        {
            _session.StateChanged -= OnStateChanged;

            Restore();
        }
    }
}