using Pursuit.Helpers;
using Pursuit.Schedulers.Helpers.Interfaces;
using Quartz;
using System.Threading.Tasks;

namespace Pursuit.Schedulers.Helpers
{
    public class DailyJob : IDailyJob
    {
        public IMSADSyncService _adService;
        public DailyJob(IMSADSyncService adService)
        {
            _adService = adService;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            //await _helperService.PerformService(BackupSchedule.Daily);

            await _adService.ADSyncService(ScheduleInterval.Daily);
        }

    }
}
