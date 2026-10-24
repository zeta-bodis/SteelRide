using System;

namespace SteelRide.Core
{
    public sealed class LevelProgress
    {
        private readonly ILevelNumberStore _store;

        public int Current { get; private set; }

        public event Action<int> Changed;

        public LevelProgress(ILevelNumberStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));

            Current = Math.Max(1, _store.Load());

            _store.Save(Current);
        }

        public void Advance()
        {
            if (Current == int.MaxValue)
                return;

            int next = Current + 1;
            _store.Save(next);

            Current = next;
            Changed?.Invoke(Current);
        }
    }
}