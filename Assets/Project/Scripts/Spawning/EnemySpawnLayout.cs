using System;

namespace SteelRide.Spawning
{
    public readonly struct EnemySpawnLayout
    {
        private readonly int _columns, _rows, _offset, _stride;
        private readonly float _width, _length, _margin, _spacing;
        private readonly uint _seed;
        public int Count { get; }

        public EnemySpawnLayout(float width, float length, float margin, float spacing, int requestedCount, int seed, int segment)
        {
            if (width <= 0 || length <= 0 || margin < 0 || spacing <= 0 || requestedCount < 0 || float.IsNaN(width + length + margin + spacing) || float.IsInfinity(width + length + margin + spacing))
                throw new ArgumentOutOfRangeException(nameof(width));

            float usableWidth = width - 2f * margin;
            float usableLength = length - 2f * margin;

            if (usableWidth < spacing || usableLength < spacing)
                throw new ArgumentException("Ground spawn area must contain at least one spacing-sized cell.");

            _width = width;
            _length = length;
            _margin = margin;
            _spacing = spacing;
            _columns = (int)(usableWidth / spacing);
            _rows = (int)(usableLength / spacing);

            int cells = checked(_columns * _rows);
            _seed = Mix(unchecked((uint)seed ^ ((uint)segment * 0x9E3779B9u)));
            _offset = (int)(_seed % (uint)cells);

            int stride = (int)(Mix(_seed + 1) % (uint)cells) + 1;

            while (Gcd(stride, cells) != 1)
                stride = stride % cells + 1;

            _stride = stride;

            Count = Math.Min(requestedCount, cells);
        }

        public void GetPosition(int ordinal, out float x, out float z)
        {
            if ((uint)ordinal >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(ordinal));

            int cell = (int)((_offset + (long)_stride * ordinal) % (_columns * _rows));
            float dx = (_width - 2f * _margin) / _columns;
            float dz = (_length - 2f * _margin) / _rows;
            uint random = Mix(unchecked(_seed + (uint)ordinal * 0x85EBCA6Bu));
            float jitterX = (Unit(random) * 2f - 1f) * (dx - _spacing) * 0.25f;
            float jitterZ = (Unit(Mix(random)) * 2f - 1f) * (dz - _spacing) * 0.25f;
            x = -_width * 0.5f + _margin + (cell % _columns + 0.5f) * dx + jitterX;
            z = _margin + (cell / _columns + 0.5f) * dz + jitterZ;
        }

        private static float Unit(uint value) => (value >> 8) * (1f / 16777216f);

        private static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int remainder = a % b;
                a = b;
                b = remainder;
            }

            return a;
        }

        private static uint Mix(uint value)
        {
            unchecked
            {
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                return value ^ (value >> 16);
            }
        }
    }
}