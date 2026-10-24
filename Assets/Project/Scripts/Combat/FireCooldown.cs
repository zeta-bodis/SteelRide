using System;

namespace SteelRide.Combat
{
    public sealed class FireCooldown
    {
        private readonly double _interval;
        private double _nextShotTime = double.NegativeInfinity;

        public FireCooldown(float shotsPerSecond)
        {
            if (float.IsNaN(shotsPerSecond) || float.IsInfinity(shotsPerSecond) || shotsPerSecond <= 0f)
                throw new ArgumentOutOfRangeException(nameof(shotsPerSecond));

            _interval = 1d / shotsPerSecond;
        }

        public bool IsReady(double time)
        {
            ValidateTime(time);
            return time >= _nextShotTime;
        }

        public void MarkFired(double time)
        {
            ValidateTime(time);
            _nextShotTime = time + _interval;
        }

        public void Reset() => _nextShotTime = double.NegativeInfinity;

        private static void ValidateTime(double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || time < 0d)
                throw new ArgumentOutOfRangeException(nameof(time));
        }
    }
}