using System;
using System.Collections.Generic;
using System.Linq;

namespace STRBlender.Core.Domain.Services
{
    /// Draws contributor proportions that sum to 1.0 while respecting per-contributor
    /// min/max bounds. Shared by the NOC game and Synthetic Profiles batch path.
    public static class ProportionSampler
    {
        public static List<double> Sample(int count, double min, double max, Random? rng = null)
        {
            rng ??= Random.Shared;

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 1)
                return new List<double> { 1.0 };

            // Clamp so a feasible solution exists when possible
            min = Math.Clamp(min, 0.01, 1.0);
            max = Math.Clamp(max, min, 1.0);

            var proportions = new List<double>(count);
            double remaining = 1.0;

            for (int i = 0; i < count - 1; i++)
            {
                double safeMax = Math.Min(max, remaining - min * (count - i - 1));
                safeMax = Math.Max(min, safeMax);

                double value = min + rng.NextDouble() * (safeMax - min);
                proportions.Add(value);
                remaining -= value;
            }

            proportions.Add(Math.Clamp(remaining, min, max));

            // Shuffle so the residual slot is not always last
            return proportions.OrderBy(_ => rng.Next()).ToList();
        }
    }
}
