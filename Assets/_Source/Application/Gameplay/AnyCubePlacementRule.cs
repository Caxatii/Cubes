using Cubes.Domain.Tower;

namespace Cubes.Application.Gameplay
{
    public sealed class AnyCubePlacementRule : ICubePlacementRule
    {
        public TowerAddResult Evaluate(CubePlacementContext context) => TowerAddResult.Added;
    }
}
