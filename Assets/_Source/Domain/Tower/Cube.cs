using System;

namespace Cubes.Domain.Tower
{
    public sealed class Cube
    {
        public Cube(int typeIndex, float horizontalOffset)
        {
            if (typeIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(typeIndex));
            }

            NumericRange.Require(horizontalOffset, -float.MaxValue, float.MaxValue, nameof(horizontalOffset));
            TypeIndex = typeIndex;
            HorizontalOffset = horizontalOffset;
        }

        public int TypeIndex { get; }
        public float HorizontalOffset { get; }
    }
}
