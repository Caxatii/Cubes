using System;

namespace Cubes.Domain.Tower
{
    public sealed class TowerSettings
    {
        public TowerSettings(float maximumHorizontalStep, float maximumBaseOffset)
        {
            NumericRange.Require(maximumHorizontalStep, 0f, 0.5f, nameof(maximumHorizontalStep));
            NumericRange.Require(maximumBaseOffset, 0f, float.MaxValue, nameof(maximumBaseOffset));
            MaximumHorizontalStep = maximumHorizontalStep;
            MaximumBaseOffset = maximumBaseOffset;
        }

        public float MaximumHorizontalStep { get; }
        public float MaximumBaseOffset { get; }
    }
}
