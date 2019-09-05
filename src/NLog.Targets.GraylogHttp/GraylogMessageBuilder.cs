﻿using System;

namespace NLog.Targets.GraylogHttp
{
    internal class GraylogMessageBuilder
    {
        private const string LevelNameProperty = "level_name";
        private readonly JsonObject _graylogMessage = new JsonObject();
        private static readonly DateTime _epochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

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

        public GraylogMessageBuilder WithLevelName(LogLevel logEventLevel)
        {
            var levelName = GetLevelName(logEventLevel);
            return WithCustomProperty(LevelNameProperty, levelName);
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

        private static string GetLevelName(LogLevel logEventLevel)
        {
            if (logEventLevel == LogLevel.Trace)
            {
                return "Trace";
            }

            if (logEventLevel == LogLevel.Debug)
            {
                return "Debug";
            }

            if (logEventLevel == LogLevel.Info)
            {
                return "Info";
            }

            if (logEventLevel == LogLevel.Warn)
            {
                return "Warn";
            }

            if (logEventLevel == LogLevel.Error)
            {
                return "Error";
            }

            if (logEventLevel == LogLevel.Fatal)
            {
                return "Fatal";
            }

            if (logEventLevel == LogLevel.Off)
            {
                return "Off";
            }

            return string.Empty;
        }

        private static readonly object GelfLevel_Critical = 2;
        private static readonly object GelfLevel_Error = 3;
        private static readonly object GelfLevel_Warning = 4;
        private static readonly object GelfLevel_Informational = 6;
        private static readonly object GelfLevel_Debug = 7;
    }
}