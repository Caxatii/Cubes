namespace Cubes.Application.Progress
{
    public interface IProgressStorage
    {
        GameProgress Load();
        void Save(GameProgress progress);
    }
}
