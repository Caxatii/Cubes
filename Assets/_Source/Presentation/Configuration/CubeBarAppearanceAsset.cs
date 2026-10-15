using System;
using UnityEngine;
using Cubes.Domain.Tower;

namespace Cubes.Presentation.Configuration
{
    [CreateAssetMenu(fileName = "CubeBarAppearance", menuName = "Cubes/Configuration/Cube Bar Appearance")]
    public sealed class CubeBarAppearanceAsset : ScriptableObject
    {
        [Serializable]
        private sealed class CubeDescription
        {
            [SerializeField] private Sprite _sprite;
            [SerializeField] private CubeColor _color;
            public Sprite Sprite => _sprite;
            public CubeColor Color => _color;
        }

        [SerializeField] private CubeDescription[] _cubes = Array.Empty<CubeDescription>();

        public int TypeCount => _cubes.Length;

        public void Validate()
        {
            if (_cubes == null || _cubes.Length == 0)
            {
                throw new InvalidOperationException("At least one cube sprite must be assigned.");
            }

            foreach (CubeDescription cube in _cubes)
            {
                if (cube == null || cube.Sprite == null || cube.Color == CubeColor.Unknown
                    || !Enum.IsDefined(typeof(CubeColor), cube.Color))
                {
                    throw new InvalidOperationException("Every cube type must have an assigned sprite.");
                }
            }
        }

        public CubeColor[] GetColors()
        {
            Validate();
            CubeColor[] colors = new CubeColor[TypeCount];

            for (int index = 0; index < colors.Length; index++)
                colors[index] = _cubes[index].Color;

            return colors;
        }

        public void ValidateCatalog(Cubes.Application.Configuration.GameConfiguration configuration)
        {
            Validate();

            if (configuration.TypeCount != TypeCount)
                throw new InvalidOperationException("Visual and gameplay type counts differ.");

            for (int index = 0; index < TypeCount; index++)
                if (_cubes[index].Color != configuration.GetColor(index))
                    throw new InvalidOperationException("Visual and gameplay colors differ at type " + index);
        }

        public Sprite GetSprite(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= _cubes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(typeIndex));
            }

            return _cubes[typeIndex].Sprite;
        }
    }
}
