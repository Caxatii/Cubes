using Cubes.Domain.Tower;

namespace Cubes.Application.Gameplay
{
    public interface ICubePlacementRule
    {
        TowerAddResult Evaluate(CubePlacementContext context);
    }
}
