using JHLib.ChartManager.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using ZLogger;

namespace JHLib.ChartManager.Report
{
    public class ChartManagerLogger
    {
        private ServiceProvider serviceProvider;
        private ILogger<ChartManagerLogger> logger;



        public ChartManagerLogger()
        {
            DirectoryInfo loggerDirectory = new DirectoryInfo(Path.Combine(DirectoryConfiguration.root, "Log"));

            if (loggerDirectory.Exists)
            {
                try
                {
                    List<FileInfo> log = loggerDirectory.GetFiles("Chart_*.log")
                                                        .OrderByDescending(file => file.Name)
                                                        .ToList();

                    if (log.Count > 100)
                    {
                        log.Skip(100)
                           .ToList()
                           .ForEach(it => it.Delete());
                    }
                }
                catch (Exception e)
                {

                }
            }
            else
            {
                loggerDirectory.Create();
            }

            ServiceCollection service = new ServiceCollection();
            service.AddLogging(builder => {
                builder.ClearProviders();
                builder.AddZLoggerRollingFile(option => {
                    option.FilePathSelector = (timeStamp, index) => Path.Combine(loggerDirectory.FullName, $"Chart_{timeStamp.UtcDateTime:yyyyMMdd}_{index}.log");
                    option.RollingInterval = ZLogger.Providers.RollingInterval.Day;
                    option.RollingSizeKB = 1024 * 1024 * 50;
                });
            });

            this.serviceProvider = service.BuildServiceProvider();
            this.logger = this.serviceProvider.GetRequiredService<ILogger<ChartManagerLogger>>();
        }



        public void Info(string message)
        {
            this.logger.ZLogInformation($"{DateTime.UtcNow:O}|{message}");
        }

        public void Error(string message)
        {
            this.logger.ZLogError($"{DateTime.UtcNow:O}|{message}");
        }
    }
}