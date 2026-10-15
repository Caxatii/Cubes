namespace Cubes.Application.Progress
{
    public interface IRecoverableProgressStorage : IProgressStorage
    {
        GameProgress LoadBackup();
        void RejectPrimary();
    }
}
