namespace SteelRide.Core
{
    public interface ILevelNumberStore
    {
        int Load();
        void Save(int level);
    }
}