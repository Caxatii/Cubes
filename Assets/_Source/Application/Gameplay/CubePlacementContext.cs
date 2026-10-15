using System.Collections.Generic;
using Cubes.Domain.Tower;

namespace Cubes.Application.Gameplay
{
    public readonly struct CubePlacementContext
    {
        public CubePlacementContext(int typeIndex, CubeColor color, int targetIndex,
            CubeColor targetColor, IReadOnlyList<Cube> cubes)
        {
            TypeIndex = typeIndex;
            Color = color;
            TargetIndex = targetIndex;
            TargetColor = targetColor;
            Cubes = cubes;
        }

        public int TypeIndex { get; }
        public CubeColor Color { get; }
        public int TargetIndex { get; }
        public CubeColor TargetColor { get; }
        public IReadOnlyList<Cube> Cubes { get; }
    }
}
