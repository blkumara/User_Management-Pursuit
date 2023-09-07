using System.Threading.Tasks;

namespace Pursuit.Schedulers.Helpers.Interfaces
{
    public interface IMSADSyncService
    {
        Task ADSyncService(string schedule);
    }
}