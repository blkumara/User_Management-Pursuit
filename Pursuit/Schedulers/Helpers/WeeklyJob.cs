using Pursuit.Schedulers.Helpers.Interfaces;
using Quartz;
using System.Threading.Tasks;

namespace Pursuit.Schedulers.Helpers
{
    public class WeeklyJob : IWeeklyJob
    {
        public IMSADSyncService _adService;
        public WeeklyJob(IMSADSyncService adService)
        {
            _adService = adService;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            await _adService.ADSyncService(BackupSchedule.Weekly);
        }
    }
}
