using Pursuit.Schedulers.Helpers;
using Pursuit.Schedulers.Helpers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using System.Configuration;
using Pursuit.Context;
using Pursuit.Model;
using ILogger = Serilog.ILogger;
using Newtonsoft.Json;
using Pursuit.Helpers;

namespace Pursuit.Schedulers
{
    public class ADSyncService : IHostedService
    {
        private readonly ILogger<ADSyncService> _logger;
        private readonly IConfiguration _config;
        //private readonly IPursuitRepository<Admin_Configuration> _adminconfigRepository;
        private readonly NameValueCollection config;
        public ADSyncService(ILogger<ADSyncService> logger, IConfiguration configs)
        {
            //SetUpNLog();
            _logger = logger;
            this._config = configs;
           // this.config = (System.Configuration.ConfigurationSection)this._config.GetSection("quartz");
           // config = (NameValueCollection)Newtonsoft.Json.JsonConvert.DeserializeObject(_config["quartz"]);
/*
           config = JsonConvert.DeserializeObject<Dictionary<string, string>>(_config["quartz"])
                     .ToNameValueCollection();*/

        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var scheduler = await GetScheduler();
                var serviceProvider = GetConfiguredServiceProvider();
                scheduler.JobFactory = new EvolveJobFactory(serviceProvider);
                await scheduler.Start();
                await ConfigureDailyJob(scheduler);
             //   await ConfigureWeeklyJob(scheduler);
             //   await ConfigureMonthlyJob(scheduler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private async Task ConfigureDailyJob(IScheduler scheduler)
        {
            var dailyJob = GetDailyJob();
            if (await scheduler.CheckExists(dailyJob.Key))
            {
                await scheduler.ResumeJob(dailyJob.Key);
                _logger.LogInformation($"The job key {dailyJob.Key} was already existed, thus resuming the same");
            }
            else
            {
                await scheduler.ScheduleJob(dailyJob, GetDailyJobTrigger());
            }
        }
/*
        private async Task ConfigureWeeklyJob(IScheduler scheduler)
        {
            var weklyJob = GetWeeklyJob();
            if (await scheduler.CheckExists(weklyJob.Key))
            {
                await scheduler.ResumeJob(weklyJob.Key);
                _logger.LogInformation($"The job key {weklyJob.Key} was already existed, thus resuming the same");
            }
            else
            {
                await scheduler.ScheduleJob(weklyJob, GetWeeklyJobTrigger());
            }
        }

        private async Task ConfigureMonthlyJob(IScheduler scheduler)
        {
            var monthlyJob = GetMonthlyJob();
            if (await scheduler.CheckExists(monthlyJob.Key))
            {
                await scheduler.ResumeJob(monthlyJob.Key);
                _logger.LogInformation($"The job key {monthlyJob.Key} was already existed, thus resuming the same");
            }
            else
            {
                await scheduler.ScheduleJob(monthlyJob, GetMonthlyJobTrigger());
            }
        }*/

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        #region "Private Functions"
        private IServiceProvider GetConfiguredServiceProvider()
        {
            var services = new ServiceCollection()
                .AddScoped<IDailyJob, DailyJob>()
                //.AddScoped<IWeeklyJob, WeeklyJob>()
               // .AddScoped<IMonthlyJob, MonthlyJob>()
                .AddScoped<IMSADSyncService, MSADSyncService>();
            return services.BuildServiceProvider();
        }
        private IJobDetail GetDailyJob()
        {
            return JobBuilder.Create<IDailyJob>()
                .WithIdentity("dailyjob", "dailygroup")
                .Build();
        }
        private ITrigger GetDailyJobTrigger()
        {
            return TriggerBuilder.Create()
                 .WithIdentity("dailytrigger", "dailygroup")
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInMinutes(5)
                     .RepeatForever())
                 .Build();
        }
      /*  private IJobDetail GetWeeklyJob()
        {
            return JobBuilder.Create<IWeeklyJob>()
                .WithIdentity("weeklyjob", "weeklygroup")
                .Build();
        }*/
      /*  private ITrigger GetWeeklyJobTrigger()
        {
            return TriggerBuilder.Create()
                 .WithIdentity("weeklytrigger", "weeklygroup")
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(120)
                     .RepeatForever())
                 .Build();
        }
        private IJobDetail GetMonthlyJob()
        {
            return JobBuilder.Create<IMonthlyJob>()
                .WithIdentity("monthlyjob", "monthlygroup")
                .Build();
        }
        private ITrigger GetMonthlyJobTrigger()
        {
            return TriggerBuilder.Create()
                 .WithIdentity("monthlytrigger", "monthlygroup")
                 .StartNow()
                 .WithSimpleSchedule(x => x
                     .WithIntervalInHours(720)
                     .RepeatForever())
                 .Build();
        }*/
        private async Task<IScheduler> GetScheduler()
        {
            // Comment this if you don't want to use database start

            //var factory = new StdSchedulerFactory(config);

            // Comment this if you don't want to use database end

            // Uncomment this if you want to use RAM instead of database start
            var props = new NameValueCollection { { "quartz.serializer.type", "binary" } };
            var factory = new StdSchedulerFactory(props);
            // Uncomment this if you want to use RAM instead of database end

            var scheduler = await factory.GetScheduler();
            return scheduler;
        }
        /* private void SetUpNLog()
         {
             var config = new NLog.Config.LoggingConfiguration();
             // Targets where to log to: File and Console
             var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "backupclientlogfile_backupservice.txt" };
             var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
             // Rules for mapping loggers to targets            
             config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);
             config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logfile);
             // Apply config           
             LogManager.Configuration = config;
             _logger = LogManager.GetCurrentClassLogger();
         }*/

        #endregion
    }
}
