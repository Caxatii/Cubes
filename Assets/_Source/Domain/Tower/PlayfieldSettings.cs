namespace Cubes.Domain.Tower
{
    public sealed class PlayfieldSettings
    {
        public PlayfieldSettings(float width, float height)
        {
            NumericRange.Require(width, 1f, float.MaxValue, nameof(width));
            NumericRange.Require(height, 1f, float.MaxValue, nameof(height));
            Width = width;
            Height = height;
        }

        public float Width { get; }
        public float Height { get; }

        public float GetX(float baseX) => 0.5f + baseX * (Width - 1f);

        public float NormalizeX(float x)
        {
            NumericRange.Require(x, 0.5f, Width - 0.5f, nameof(x));
            return Width > 1f ? (x - 0.5f) / (Width - 1f) : 0.5f;
        }
    }
}
