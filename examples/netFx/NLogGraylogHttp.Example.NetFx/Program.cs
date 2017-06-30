using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace NLogGraylogHttp.Example.NetFx
{
    class Program
    {
        static void Main(string[] args)
        {
            var nLogLogger = LogManager.GetCurrentClassLogger();

            while (true)
            {
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
