using System;
using System.Collections.Generic;
using Cubes.Domain.Tower;

namespace Cubes.Application.Configuration
{
    public sealed class GameConfiguration
    {
        private readonly TowerSettings _tower;
        private readonly PlayfieldSettings _playfield;

        public GameConfiguration(IReadOnlyList<CubeColor> colors, float maximumHorizontalStep,
            float maximumBaseOffset, float playfieldWidth, float playfieldHeight)
        {
            if (colors == null || colors.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(colors));
            }

            _tower = new TowerSettings(maximumHorizontalStep, maximumBaseOffset);
            _playfield = new PlayfieldSettings(playfieldWidth, playfieldHeight);
            _colors = new CubeColor[colors.Count];

            for (int index = 0; index < colors.Count; index++)
            {
                if (colors[index] == CubeColor.Unknown || !Enum.IsDefined(typeof(CubeColor), colors[index]))
                    throw new ArgumentOutOfRangeException(nameof(colors));
                _colors[index] = colors[index];
            }
        }

        private readonly CubeColor[] _colors;
        public int TypeCount => _colors.Length;
        public CubeColor GetColor(int typeIndex) => _colors[typeIndex];
        public float PlayfieldWidth => _playfield.Width;
        public float PlayfieldHeight => _playfield.Height;

        internal TowerModel CreateTower() => new TowerModel(_tower, _playfield);

        internal bool HasType(int typeIndex) => typeIndex >= 0 && typeIndex < TypeCount;
    }
}
