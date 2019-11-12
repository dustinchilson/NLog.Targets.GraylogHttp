using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace NLogGraylogHttp.Example.NetCore
{
    internal static class Program
    {
        public static void Main()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            LogManager.LoadConfiguration("NLog.config");

            serviceProvider
                .GetService<ILoggerFactory>()
                .AddNLog();

            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger(nameof(Program));
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