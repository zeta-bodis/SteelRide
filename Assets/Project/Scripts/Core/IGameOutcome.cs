namespace SteelRide.Core
{
    public interface IGameOutcome
    {
        bool TryWin();
        bool TryLose();
    }
}