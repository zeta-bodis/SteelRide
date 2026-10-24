using System;

namespace SteelRide.Spawning
{
    public sealed class FixedPool<T> where T : class
    {
        private readonly T[] _items;
        private readonly int[] _free;
        private readonly bool[] _rented;
        private int _freeCount;

        public int Capacity => _items.Length;
        public int AvailableCount => _freeCount;

        public FixedPool(int capacity, Func<int, T> factory)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _items = new T[capacity];
            _free = new int[capacity];
            _rented = new bool[capacity];

            for (int i = 0; i < capacity; i++)
            {
                _items[i] = factory(i) ?? throw new InvalidOperationException("Pool factory returned null.");
                _free[i] = capacity - 1 - i;
            }

            _freeCount = capacity;
        }

        public T ItemAt(int index) => _items[index];

        public bool IsRented(int index) => _rented[index];

        public bool TryRent(out T item, out int index)
        {
            if (_freeCount == 0)
            {
                item = null;
                index = -1;
                return false;
            }

            index = _free[--_freeCount];
            _rented[index] = true;
            item = _items[index];

            return true;
        }

        public bool TryReturn(int index)
        {
            if ((uint)index >= (uint)Capacity || !_rented[index])
                return false;

            _rented[index] = false;
            _free[_freeCount++] = index;
            return true;
        }
    }
}