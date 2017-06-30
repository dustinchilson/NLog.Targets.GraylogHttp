using System;
using System.Linq;
using NLog.Config;
using NLog.Layouts;

namespace NLog.Targets.GraylogHttp
{
    [NLogConfigurationItem]
    public class GraylogParameterInfo
    {
        [RequiredParameter]
        public string Name { get; set; }

        [RequiredParameter]
        public Layout Layout { get; set; }
    }
}