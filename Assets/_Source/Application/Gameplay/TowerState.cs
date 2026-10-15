using System;
using System.Collections.Generic;
using Cubes.Domain.Tower;

namespace Cubes.Application.Gameplay
{
    public sealed class TowerState
    {
        public TowerState(float playfieldWidth, float playfieldHeight, float baseX, IEnumerable<Cube> cubes,
            float referenceWidth = 0f, float referenceHeight = 0f)
        {
            PlayfieldWidth = playfieldWidth;
            PlayfieldHeight = playfieldHeight;
            ReferenceWidth = referenceWidth == 0f ? playfieldWidth : referenceWidth;
            ReferenceHeight = referenceHeight == 0f ? playfieldHeight : referenceHeight;
            BaseX = baseX;
            Cubes = new List<Cube>(cubes ?? throw new ArgumentNullException(nameof(cubes))).AsReadOnly();
        }

        public float PlayfieldWidth { get; }
        public float PlayfieldHeight { get; }
        public float ReferenceWidth { get; }
        public float ReferenceHeight { get; }
        public float BaseX { get; }
        public IReadOnlyList<Cube> Cubes { get; }

        internal static TowerState Capture(TowerModel tower, float referenceWidth, float referenceHeight)
            => new TowerState(tower.Playfield.Width, tower.Playfield.Height, tower.BaseX, tower.Cubes, referenceWidth, referenceHeight);
    }
}
