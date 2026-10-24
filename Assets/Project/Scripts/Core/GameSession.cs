using System;

namespace SteelRide.Core
{
    public sealed class GameSession : IGameStateSource, IGameOutcome
    {
        public GameState State { get; private set; } = GameState.Ready;

        public event Action<GameState> StateChanged;

        public bool TryStart()
        {
            return TryTransition(GameState.Ready, GameState.Playing);
        }

        public bool TryWin()
        {
            return TryTransition(GameState.Playing, GameState.Won);
        }

        public bool TryLose()
        {
            return TryTransition(GameState.Playing, GameState.Lost);
        }

        public bool TryPause() => TryTransition(GameState.Playing, GameState.Paused);

        public bool TryResume() => TryTransition(GameState.Paused, GameState.Playing);

        public bool TryReset()
        {
            if (State != GameState.Won && State != GameState.Lost)
                return false;

            return TryTransition(State, GameState.Ready);
        }

        private bool TryTransition(GameState requiredState, GameState nextState)
        {
            if (State != requiredState)
                return false;

            State = nextState;

            StateChanged?.Invoke(State);

            return true;
        }
    }
}