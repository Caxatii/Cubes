using System;
using Cubes.Application.Configuration;
using Cubes.Domain.Tower;

namespace Cubes.Application.Gameplay
{
    public sealed class GameSession
    {
        private readonly GameConfiguration _configuration;
        private readonly TowerModel _tower;
        private readonly Random _random;
        private readonly ICubePlacementRule _placementRule;
        public bool IsHeightLimitReached => _tower.IsFull;

        public GameSession(GameConfiguration configuration) : this(configuration, new Random()) { }

        public GameSession(GameConfiguration configuration, Random random, ICubePlacementRule placementRule = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _placementRule = placementRule ?? new AnyCubePlacementRule();
            _tower = configuration.CreateTower();
        }

        public TowerState CreateSnapshot() => TowerState.Capture(_tower, _configuration.PlayfieldWidth, _configuration.PlayfieldHeight);

        public TowerState ResizePlayfield(float width, float height)
        {
            _tower.ResizePlayfield(width, height);
            return CreateSnapshot();
        }

        public void PlaceFirst(int typeIndex, float x)
        {
            RequireType(typeIndex);
            _tower.PlaceFirst(typeIndex, x);
        }

        public TowerAddResult AddNext(int typeIndex) => AddNext(typeIndex, _tower.Cubes.Count - 1);

        public TowerAddResult AddNext(int typeIndex, int targetIndex)
        {
            RequireType(typeIndex);

            if (targetIndex < 0 || targetIndex >= _tower.Cubes.Count)
                return TowerAddResult.InvalidTarget;

            if (_tower.IsFull)
                return TowerAddResult.LimitReached;

            Cube target = _tower.Cubes[targetIndex];

            TowerAddResult result = _placementRule.Evaluate(new CubePlacementContext(typeIndex,
                _configuration.GetColor(typeIndex), targetIndex, _configuration.GetColor(target.TypeIndex), _tower.Cubes));

            if (result != TowerAddResult.Added)
                return result;

            if (targetIndex >= _tower.Cubes.Count || !ReferenceEquals(target, _tower.Cubes[targetIndex]))
                return TowerAddResult.InvalidTarget;

            return _tower.AddNext(typeIndex, _random);
        }

        public bool Remove(int index) => _tower.Remove(index);
        public void Reset() => _tower.Clear();

        public void Restore(TowerState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.ReferenceWidth != _configuration.PlayfieldWidth ||
                state.ReferenceHeight != _configuration.PlayfieldHeight)
            {
                throw new ArgumentException("The saved tower uses different reference dimensions.", nameof(state));
            }

            foreach (Cube cube in state.Cubes)
            {
                if (cube == null)
                {
                    throw new ArgumentException("The saved tower contains a missing cube.", nameof(state));
                }

                RequireType(cube.TypeIndex);
            }

            _tower.Restore(state.Cubes, state.BaseX, new PlayfieldSettings(state.PlayfieldWidth, state.PlayfieldHeight));
        }

        private void RequireType(int typeIndex)
        {
            if (!_configuration.HasType(typeIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(typeIndex));
            }
        }
    }
}
