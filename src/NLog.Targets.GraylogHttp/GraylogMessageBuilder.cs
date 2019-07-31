using System;

namespace NLog.Targets.GraylogHttp
{
    internal class GraylogMessageBuilder
    {
        private readonly JsonObject _graylogMessage = new JsonObject();
        private static readonly DateTime _epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public GraylogMessageBuilder WithLevel(LogLevel level)
        {
            object graylogLevel;

            if (level == LogLevel.Trace)
                graylogLevel = GelfLevel_Debug;
            else if (level == LogLevel.Debug)
                graylogLevel = GelfLevel_Debug;
            else if (level == LogLevel.Info)
                graylogLevel = GelfLevel_Informational;
            else if (level == LogLevel.Warn)
                graylogLevel = GelfLevel_Warning;
            else if (level == LogLevel.Fatal)
                graylogLevel = GelfLevel_Critical;
            else
                graylogLevel = GelfLevel_Error;

            return WithProperty("level", graylogLevel);
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
            WithProperty("timestamp", (long)(timestamp.ToUniversalTime() - _epochTime).TotalMilliseconds);
            WithProperty("version", "1.1");
            return _graylogMessage.ToString();
        }

        private static readonly object GelfLevel_Critical = 2;
        private static readonly object GelfLevel_Error = 3;
        private static readonly object GelfLevel_Warning = 4;
        private static readonly object GelfLevel_Informational = 6;
        private static readonly object GelfLevel_Debug = 7;
    }
}