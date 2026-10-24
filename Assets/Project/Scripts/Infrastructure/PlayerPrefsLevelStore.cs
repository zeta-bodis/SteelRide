using SteelRide.Core;
using UnityEngine;

namespace SteelRide.Infrastructure
{
    public sealed class PlayerPrefsLevelStore : ILevelNumberStore
    {
        public const string Key = "TurretRunner.CurrentLevel";

        public int Load() => PlayerPrefs.GetInt(Key, 1);

        public void Save(int level)
        {
            if (PlayerPrefs.HasKey(Key) && PlayerPrefs.GetInt(Key) == level)
                return;

            PlayerPrefs.SetInt(Key, level);
            PlayerPrefs.Save();
        }
    }
}