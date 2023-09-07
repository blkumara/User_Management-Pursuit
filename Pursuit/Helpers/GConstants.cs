using Pursuit.Context;
using Pursuit.Context.AD;
using Pursuit.Model;

namespace Pursuit.Helpers
{
    public static class GConstants
    {
        public static readonly string NoTimeConfiguredInSettings = "There is no time configured in the appsettings";
        public static readonly string NoFolderToZipLocationSettings = "There is no folder to zip configured in the appsettings";
        public static readonly string NoStorageConnectionStringSettings = "There is no storage connection string configured in the appsettings";
        public static readonly string NoFolderFromZipLocationSettings = "There is no folder from zip configured in the appsettings";
    }

    public static class ScheduleInterval
    {
        public static readonly string Daily = "daily";
        public static readonly string Weekly = "weekly";
        public static readonly string Monthly = "monthly";
    }

    public delegate IADRepository<ADRecord> ServiceResolver(string key);
    public delegate IDeltaRepository<DeltaModel> DeltaServiceResolver(string key);
}
