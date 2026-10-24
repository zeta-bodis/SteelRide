using System;

namespace SteelRide.Core
{
    public interface IGameStateSource
    {
        GameState State { get; }

        event Action<GameState> StateChanged;
    }
}