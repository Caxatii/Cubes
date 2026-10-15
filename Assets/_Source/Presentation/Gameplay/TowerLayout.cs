using Cubes.Domain.Tower;
using System;
using Cubes.Application.Gameplay;
using UnityEngine;

namespace Cubes.Presentation.Gameplay
{
    public readonly struct TowerLayout
    {
        private readonly TowerState _state;
        private readonly Rect _bounds;

        public TowerLayout(TowerState state, Rect bounds, Vector2 prefabSize)
        {
            if (prefabSize.x <= 0f || prefabSize.y <= 0f || bounds.width <= 0f || bounds.height <= 0f)
            {
                throw new InvalidOperationException("The playfield cannot be displayed in an empty area.");
            }

            _state = state;
            _bounds = bounds;
            float minimum = 0f;
            float maximum = 0f;

            foreach (Cube cube in state.Cubes)
            {
                minimum = Mathf.Min(minimum, cube.HorizontalOffset);
                maximum = Mathf.Max(maximum, cube.HorizontalOffset);
            }

            float width = Mathf.Max(state.ReferenceWidth, 1f + maximum - minimum + 0.00001f);
            float scale = Mathf.Min(bounds.width / (width * prefabSize.x),
                bounds.height / (state.ReferenceHeight * prefabSize.y));
            CubeSize = prefabSize * scale;

            if (CubeSize.x < 1f || CubeSize.y < 1f)
            {
                throw new InvalidOperationException("The playfield is too small to display a cube.");
            }

            LogicalSize = new Vector2(Mathf.Max(1f, bounds.width / CubeSize.x), Mathf.Max(1f, bounds.height / CubeSize.y));
        }

        public Vector2 CubeSize { get; }
        public Vector2 LogicalSize { get; }

        public Vector2 ToLocal(Vector2 logicalPosition) => _bounds.min + Vector2.Scale(logicalPosition, CubeSize);

        public Vector2 ToLogical(Vector2 localPosition)
        {
            Vector2 relative = localPosition - _bounds.min;
            return new Vector2(relative.x / CubeSize.x, relative.y / CubeSize.y);
        }

        public Vector2 GetCubePosition(int index)
        {
            return ToLocal(new Vector2(0.5f + _state.BaseX * (LogicalSize.x - 1f) + _state.Cubes[index].HorizontalOffset,
                0.5f + index));
        }
    }
}
