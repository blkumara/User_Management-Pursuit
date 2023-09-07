namespace Pursuit.Schedulers
{
    public interface IBackupService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}