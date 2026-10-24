using System;

namespace SteelRide.Effects
{
    public sealed class DefeatTimeline
    {
        private readonly float _flipDuration, _explosionDelay, _minimumExplosionTime, _timeout;
        private float _explosionTime;

        public float Elapsed { get; private set; }
        public float FlipProgress => Math.Min(1f, Elapsed / _flipDuration);
        public bool ExplosionStarted { get; private set; }
        public bool IsComplete { get; private set; }

        public DefeatTimeline(float flipDuration, float explosionDelay, float minimumExplosionTime, float timeout)
        {
            if (!Finite(flipDuration) || !Finite(explosionDelay) || !Finite(minimumExplosionTime) || !Finite(timeout) || flipDuration <= 0f || explosionDelay < 0f || minimumExplosionTime < 0f || timeout < Math.Max(flipDuration, explosionDelay + minimumExplosionTime))
                throw new ArgumentOutOfRangeException(nameof(timeout));

            _flipDuration = flipDuration;
            _explosionDelay = explosionDelay;
            _minimumExplosionTime = minimumExplosionTime;
            _timeout = timeout;
        }

        public void Reset()
        {
            Elapsed = _explosionTime = 0f;
            ExplosionStarted = IsComplete = false;
        }

        public bool Step(float deltaTime, bool explosionAlive)
        {
            if (!Finite(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            if (IsComplete || deltaTime == 0f)
                return false;

            Elapsed += deltaTime;

            if (!ExplosionStarted && Elapsed >= _explosionDelay)
            {
                ExplosionStarted = true;

                _explosionTime = Elapsed;

                return true;
            }

            bool presentationFinished = FlipProgress >= 1f && ExplosionStarted && Elapsed - _explosionTime >= _minimumExplosionTime && !explosionAlive;
            bool timedOut = ExplosionStarted && Elapsed >= Math.Max(_timeout, _explosionTime + _minimumExplosionTime);

            IsComplete = presentationFinished || timedOut;
            return false;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}