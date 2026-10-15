namespace Cubes.Application.Configuration
{
    public interface IGameConfigurationSource
    {
        GameConfiguration CreateConfiguration();
    }
}
