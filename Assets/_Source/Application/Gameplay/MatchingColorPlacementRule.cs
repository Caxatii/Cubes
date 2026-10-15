using Cubes.Domain.Tower;

namespace Cubes.Application.Gameplay
{
    public sealed class MatchingColorPlacementRule : ICubePlacementRule
    {
        public TowerAddResult Evaluate(CubePlacementContext context)
            => context.Color == context.TargetColor ? TowerAddResult.Added : TowerAddResult.ColorMismatch;
    }
}
