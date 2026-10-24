using System;

namespace SteelRide.Vehicle
{
    public sealed class SinusoidalDriveMotion
    {
        private float _amplitude;
        private float _wavelength;

        public double Distance { get; private set; }

        private double Phase => (Distance % _wavelength) / _wavelength * (2d * Math.PI);

        public float LateralOffset => (float)(_amplitude * Math.Sin(Phase));
        public float LateralSlope => (float)(_amplitude * (2d * Math.PI / _wavelength) * Math.Cos(Phase));

        public SinusoidalDriveMotion(float amplitude, float wavelength)
        {
            Configure(amplitude, wavelength);
        }

        public void Configure(float amplitude, float wavelength)
        {
            if (float.IsNaN(amplitude) || float.IsInfinity(amplitude) || amplitude < 0f)
                throw new ArgumentOutOfRangeException(nameof(amplitude));

            if (float.IsNaN(wavelength) || float.IsInfinity(wavelength) || wavelength <= 0f)
                throw new ArgumentOutOfRangeException(nameof(wavelength));

            _amplitude = amplitude;
            _wavelength = wavelength;
        }

        public void Advance(double distance)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0d || double.IsInfinity(Distance + distance))
                throw new ArgumentOutOfRangeException(nameof(distance));

            Distance += distance;
        }

        public void Reset() => Distance = 0d;
    }
}