using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;

namespace NLogGraylogHttp.Example.NetCore
{
    class Program
    {
        public static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            LogManager.LoadConfiguration("NLog.config");

            serviceProvider
                .GetService<ILoggerFactory>()
                .AddNLog();
            
            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();
            var nLogLogger = LogManager.GetCurrentClassLogger();

            while (true)
            {
                logger.LogTrace("Extensions.Logging - LogTrace");
                logger.LogDebug("Extensions.Logging - LogDebug");
                logger.LogInformation("Extensions.Logging - LogInformation");
                logger.LogWarning("Extensions.Logging - LogWarning");
                logger.LogError("Extensions.Logging - LogError");
                logger.LogCritical("Extensions.Logging - LogCritical");

                nLogLogger.Trace("NLog Logging - Trace");
                nLogLogger.Debug("NLog Logging - Debug");
                nLogLogger.Info("NLog Logging - Info");
                nLogLogger.Warn("NLog Logging - Warn");
                nLogLogger.Error("NLog Logging - Error");
                nLogLogger.Fatal("NLog Logging - Fatal");
            }
        }
    }
}