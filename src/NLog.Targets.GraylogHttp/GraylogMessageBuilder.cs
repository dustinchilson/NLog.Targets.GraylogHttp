using System;

namespace NLog.Targets.GraylogHttp
{
    internal class GraylogMessageBuilder
    {
        private const string SyslogLevelPropertyName = "syslog_level_name";
        private const string NLogLogLevelPropertyName = "nlog_level_name";
        private readonly JsonObject _graylogMessage = new JsonObject();
        private static readonly DateTime _epochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public GraylogMessageBuilder WithLevel(LogLevel level)
        {
            byte graylogLevel;

            if (level == LogLevel.Trace)
                graylogLevel = GelfLevel_Debug;
            else if (level == LogLevel.Debug)
                graylogLevel = GelfLevel_Debug;
            else if (level == LogLevel.Info)
                graylogLevel = GelfLevel_Informational;
            else if (level == LogLevel.Warn)
                graylogLevel = GelfLevel_Warning;
            else if (level == LogLevel.Error)
                graylogLevel = GelfLevel_Error;
            else if (level == LogLevel.Fatal)
                graylogLevel = GelfLevel_Critical;
            else
                graylogLevel = GelfLevel_Debug;

            return WithProperty("level", graylogLevel);
        }

        public GraylogMessageBuilder WithSyslogLevelName(LogLevel logEventLevel)
        {
            var levelName = GetSyslogLevelName(logEventLevel);
            return WithCustomProperty(SyslogLevelPropertyName, levelName);
        }

        public GraylogMessageBuilder WithNLogLevelName(LogLevel logEventLevel)
        {
            var levelName = GetNLogLevelName(logEventLevel);
            return WithCustomProperty(NLogLogLevelPropertyName, levelName);
        }

        public GraylogMessageBuilder WithProperty(string propertyName, object value)
        {
            if (value is IConvertible primitive && primitive.GetTypeCode() != TypeCode.String)
            {
                value = primitive;
            }
            else
            {
                // Trunate due to https://github.com/Graylog2/graylog2-server/issues/873
                // 32766b max, C# strings 2b per char
                value = value?.ToString()?.Truncate(16383);
            }

            _graylogMessage[propertyName] = value;
            return this;
        }

        public GraylogMessageBuilder WithCustomProperty(string propertyName, object value)
        {
            if (!propertyName.StartsWith("_", StringComparison.Ordinal))
                propertyName = string.Concat("_", propertyName);

            return WithProperty(propertyName, value);
        }

        public string Render(DateTime timestamp)
        {
            WithProperty("timestamp", (decimal)(timestamp.ToUniversalTime() - _epochTime).TotalSeconds);
            WithProperty("version", "1.1");
            return _graylogMessage.ToString();
        }

        private static string GetSyslogLevelName(LogLevel logEventLevel)
        {
            if (logEventLevel == null)
            {
                return string.Empty;
            }

            if (logEventLevel == LogLevel.Trace)
            {
                return "Debug";
            }

            if (logEventLevel == LogLevel.Debug)
            {
                return "Debug";
            }

            if (logEventLevel == LogLevel.Info)
            {
                return "Informational";
            }

            if (logEventLevel == LogLevel.Warn)
            {
                return "Warning";
            }

            if (logEventLevel == LogLevel.Error)
            {
                return "Error";
            }

            if (logEventLevel == LogLevel.Fatal)
            {
                return "Critical";
            }

            if (logEventLevel == LogLevel.Off)
            {
                return "Debug";
            }

            return "Debug";
        }

        private static string GetNLogLevelName(LogLevel logEventLevel)
        {
            if (logEventLevel == null)
            {
                return string.Empty;
            }

            return logEventLevel.Name;
        }

        private const byte GelfLevel_Critical = 2;
        private const byte GelfLevel_Error = 3;
        private const byte GelfLevel_Warning = 4;
        private const byte GelfLevel_Informational = 6;
        private const byte GelfLevel_Debug = 7;
    }
}