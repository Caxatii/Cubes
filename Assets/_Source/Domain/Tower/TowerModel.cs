using System;
using System.Collections.Generic;

namespace Cubes.Domain.Tower
{
    public sealed class TowerModel
    {
        private const float OffsetTolerance = 0.000001f;
        private readonly List<Cube> _cubes = new List<Cube>();
        private readonly IReadOnlyList<Cube> _readOnlyCubes;

        public TowerModel(TowerSettings settings, PlayfieldSettings playfield)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Playfield = playfield ?? throw new ArgumentNullException(nameof(playfield));
            _readOnlyCubes = _cubes.AsReadOnly();
        }

        public TowerSettings Settings { get; }
        public PlayfieldSettings Playfield { get; private set; }
        public IReadOnlyList<Cube> Cubes => _readOnlyCubes;
        public float BaseX { get; private set; }
        public bool IsFull => _cubes.Count > Playfield.Height;

        public void ResizePlayfield(float width, float height)
        {
            PlayfieldSettings playfield = new PlayfieldSettings(width, height);
            float minimum = 0f;
            float maximum = 0f;

            foreach (Cube cube in _cubes)
            {
                minimum = Math.Min(minimum, cube.HorizontalOffset);
                maximum = Math.Max(maximum, cube.HorizontalOffset);
            }

            float left = 0.5f - minimum;
            float right = width - 0.5f - maximum;

            if (right < left - OffsetTolerance)
            {
                throw new ArgumentException("The tower does not fit the resized playfield.");
            }

            float x = Math.Max(left, Math.Min(Math.Max(left, right), playfield.GetX(BaseX)));
            BaseX = playfield.NormalizeX(x);
            Playfield = playfield;
        }

        public void PlaceFirst(int typeIndex, float x)
        {
            if (_cubes.Count != 0)
            {
                throw new InvalidOperationException("The tower already has a first cube.");
            }

            Cube cube = new Cube(typeIndex, 0f);
            NumericRange.Require(x, 0f, Playfield.Width, nameof(x));
            x = Math.Max(0.5f, Math.Min(Playfield.Width - 0.5f, x));

            BaseX = Playfield.NormalizeX(x);
            _cubes.Add(cube);
        }

        public TowerAddResult AddNext(int typeIndex, Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (IsFull)
            {
                return TowerAddResult.LimitReached;
            }

            if (Cubes.Count == 0)
            {
                throw new InvalidOperationException("Place the first cube before adding to the tower.");
            }

            float baseX = Playfield.GetX(BaseX);
            float previous = Cubes[Cubes.Count - 1].HorizontalOffset;
            float minimum = Math.Max(0.5f - baseX, Math.Max(-Settings.MaximumBaseOffset, previous - Settings.MaximumHorizontalStep));
            float maximum = Math.Min(Playfield.Width - 0.5f - baseX, Math.Min(Settings.MaximumBaseOffset, previous + Settings.MaximumHorizontalStep));

            if (minimum > maximum)
            {
                return TowerAddResult.InvalidOffset;
            }

            float offset = (float)(minimum + random.NextDouble() * ((double)maximum - minimum));
            _cubes.Add(new Cube(typeIndex, offset));
            return TowerAddResult.Added;
        }

        public bool Remove(int index)
        {
            if (index < 0 || index >= _cubes.Count)
            {
                return false;
            }

            bool first = index == 0;
            _cubes.RemoveAt(index);

            if (_cubes.Count == 0)
            {
                BaseX = default;
            }
            else if (first)
            {
                float offset = _cubes[0].HorizontalOffset;
                float x = Playfield.GetX(BaseX) + offset;
                BaseX = Playfield.NormalizeX(Math.Max(0.5f, Math.Min(Playfield.Width - 0.5f, x)));

                for (int remaining = 0; remaining < _cubes.Count; remaining++)
                {
                    Cube cube = _cubes[remaining];
                    _cubes[remaining] = new Cube(cube.TypeIndex, cube.HorizontalOffset - offset);
                }
            }

            return true;
        }

        public void Clear()
        {
            _cubes.Clear();
            BaseX = default;
        }

        public void Restore(IEnumerable<Cube> cubes, float baseX, PlayfieldSettings playfield = null)
        {
            if (cubes == null)
            {
                throw new ArgumentNullException(nameof(cubes));
            }

            playfield = playfield ?? Playfield;
            NumericRange.Require(baseX, 0f, 1f, nameof(baseX));
            List<Cube> restored = new List<Cube>();

            foreach (Cube cube in cubes)
            {
                if (cube == null)
                {
                    throw new ArgumentException("The saved tower contains invalid cubes.", nameof(cubes));
                }

                NumericRange.Require(cube.HorizontalOffset, -float.MaxValue, float.MaxValue, nameof(cubes));
                float x = playfield.GetX(baseX) + cube.HorizontalOffset;

                if (x < 0.5f - OffsetTolerance || x > playfield.Width - 0.5f + OffsetTolerance
                    || (restored.Count == 0 && cube.HorizontalOffset != 0f))
                {
                    throw new ArgumentException("The saved tower lies outside its playfield.", nameof(cubes));
                }

                restored.Add(cube);
            }

            _cubes.Clear();
            _cubes.AddRange(restored);
            Playfield = playfield;
            BaseX = restored.Count == 0 ? 0f : baseX;
        }
    }
}
