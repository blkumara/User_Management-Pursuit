using Pursuit.Schedulers.Helpers.Interfaces;
using Quartz;
using System.Threading.Tasks;

namespace Pursuit.Schedulers.Helpers
{
    public class MonthlyJob : IMonthlyJob
    {
        public IMSADSyncService _adService;
        public MonthlyJob(IMSADSyncService adService)
        {
            _adService = adService;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            await _adService.ADSyncService(BackupSchedule.Monthly);
        }
    }
}
