using System;
using System.Collections.Generic;
using System.Linq;

namespace NLog.Targets.GraylogHttp
{
    internal class GraylogMessageBuilder
    {
        private readonly JsonObject _graylogMessage = new JsonObject();
        private readonly Func<int> _timestampGenerator;

        internal GraylogMessageBuilder(Func<int> timestampGenerator = null)
        {
            if (timestampGenerator == null)
                timestampGenerator = () => (int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            _timestampGenerator = timestampGenerator;
        }

        public GraylogMessageBuilder WithLevel(LogLevel level)
        {
            GelfLevel graylogLevel;

            if (level == LogLevel.Debug)
                graylogLevel = GelfLevel.Debug;
            else if (level == LogLevel.Fatal)
                graylogLevel = GelfLevel.Critical;
            else if (level == LogLevel.Info)
                graylogLevel = GelfLevel.Informational;
            else if (level == LogLevel.Trace)
                graylogLevel = GelfLevel.Informational;
            else
                graylogLevel = level == LogLevel.Warn ? GelfLevel.Warning : GelfLevel.Error;

            return this.WithProperty("level", (int)graylogLevel);
        }

        public GraylogMessageBuilder WithProperty(string propertyName, object value)
        {
            // Trunate due to https://github.com/Graylog2/graylog2-server/issues/873
            // 32766b max, C# strings 2b per char
            _graylogMessage[propertyName] = value.ToString().Truncate(16383);
            return this;
        }

        public GraylogMessageBuilder WithCustomProperty(string propertyName, object value)
        {
            return this.WithProperty($"_{propertyName}", value);
        }

        public GraylogMessageBuilder WithCustomPropertyRange(Dictionary<string, string> properties)
        {
            return properties.Aggregate(this, (builder, pair) => builder.WithCustomProperty(pair.Key, pair.Value));
        }

        public string Render()
        {
            this.WithProperty("timestamp", _timestampGenerator());
            this.WithProperty("version", "1.1");

            return _graylogMessage.ToString();
        }

        public enum GelfLevel
        {
            Emergency = 0,
            Alert = 1,
            Critical = 2,
            Error = 3,
            Warning = 4,
            Notice = 5,
            Informational = 6,
            Debug = 7
        }
    }
}