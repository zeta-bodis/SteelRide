using System;

namespace SteelRide.Turret
{
    public sealed class YawMotor
    {
        private readonly float _minAngle;
        private readonly float _maxAngle;
        private readonly float _degreesPerSecond;
        public float Angle { get; private set; }

        public YawMotor(float minAngle, float maxAngle, float degreesPerSecond, float initialAngle = 0f)
        {
            RequireFinite(minAngle, nameof(minAngle));
            RequireFinite(maxAngle, nameof(maxAngle));
            RequireFinite(degreesPerSecond, nameof(degreesPerSecond));
            RequireFinite(initialAngle, nameof(initialAngle));

            if (minAngle > maxAngle)
                throw new ArgumentException("Minimum yaw cannot exceed maximum yaw.");

            if (degreesPerSecond < 0f)
                throw new ArgumentOutOfRangeException(nameof(degreesPerSecond));

            _minAngle = minAngle;
            _maxAngle = maxAngle;
            _degreesPerSecond = degreesPerSecond;

            Angle = Clamp(initialAngle);
        }

        public float StepTowards(float requestedAngle, float deltaTime)
        {
            RequireFinite(requestedAngle, nameof(requestedAngle));
            RequireFinite(deltaTime, nameof(deltaTime));

            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            float targetAngle = Clamp(requestedAngle);
            float delta = targetAngle - Angle;
            float step = Math.Min(Math.Abs(delta), _degreesPerSecond * deltaTime);

            Angle = Clamp(Angle + Math.Sign(delta) * step);
            return Angle;
        }

        public void Reset() => Angle = Clamp(0f);

        private float Clamp(float angle) => Math.Max(_minAngle, Math.Min(_maxAngle, angle));

        private static void RequireFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}