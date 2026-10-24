using System;

namespace SteelRide.Core
{
    public readonly struct RunDistance
    {
        public float Total { get; }
        public float Travelled { get; }
        public float Remaining => Total - Travelled;
        public float Normalized => Travelled / Total;

        public RunDistance(float startZ, float finishZ, float currentZ)
        {
            if (float.IsNaN(startZ) || float.IsInfinity(startZ) || float.IsNaN(finishZ) || float.IsInfinity(finishZ) || float.IsNaN(currentZ) || float.IsInfinity(currentZ) || finishZ <= startZ || float.IsInfinity(finishZ - startZ))
                throw new ArgumentOutOfRangeException(nameof(finishZ));

            Total = finishZ - startZ;
            Travelled = Math.Max(0f, Math.Min(Total, currentZ - startZ));
        }
    }
}
