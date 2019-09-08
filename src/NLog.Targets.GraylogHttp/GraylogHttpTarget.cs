using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog.Common;
using NLog.Config;
using Polly;
using Polly.CircuitBreaker;

namespace NLog.Targets.GraylogHttp
{
    [Target("GraylogHttp")]
    public class GraylogHttpTarget : TargetWithContext
    {
        private HttpClient _httpClient;
        private Uri _requestAddress;
        private CircuitBreakerPolicy _policy;

        public GraylogHttpTarget()
        {
            ContextProperties = new List<TargetPropertyWithContext>();
        }

        [RequiredParameter]
        public string GraylogServer { get; set; }

        public string GraylogPort { get; set; }

        public string Facility { get; set; }

        public string Host { get; set; }

        public int FailureThreshold { get; set; } = 50;

        public int SamplingDurationSeconds { get; set; } = 10;

        public int MinimumThroughput { get; set; } = 10;

        public int FailureCooldownSeconds { get; set; } = 30;

        [ArrayParameter(typeof(TargetPropertyWithContext), "parameter")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "NLog Behavior")]
        public override IList<TargetPropertyWithContext> ContextProperties { get; }

        protected override void InitializeTarget()
        {
            if (string.IsNullOrEmpty(Host))
                Host = GetMachineName();

            _httpClient = new HttpClient();

            _requestAddress = new Uri(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}/gelf", GraylogServer));

            if (!string.IsNullOrEmpty(GraylogPort))
                _requestAddress = new Uri(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}/gelf", GraylogServer, GraylogPort));

            _httpClient.DefaultRequestHeaders.ExpectContinue = false; // Expect (100) Continue breaks the graylog server

            _policy = Policy
                .Handle<Exception>()
                .AdvancedCircuitBreaker(
                    (double)FailureThreshold / 100,
                    TimeSpan.FromSeconds(SamplingDurationSeconds),
                    MinimumThroughput,
                    TimeSpan.FromSeconds(FailureCooldownSeconds));

            // Prefix the custom properties with underscore upfront, so we don't have to do it for each log-event
            foreach (TargetPropertyWithContext p in ContextProperties)
            {
                if (!p.Name.StartsWith("_", StringComparison.Ordinal))
                    p.Name = string.Concat("_", p.Name);
            }

            base.InitializeTarget();
        }

        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent == null)
                return;

            GraylogMessageBuilder messageBuilder = new GraylogMessageBuilder()
                .WithProperty("short_message", logEvent.FormattedMessage)
                .WithProperty("host", Host)
                .WithLevel(logEvent.Level)
                .WithSyslogLevelName(logEvent.Level)
                .WithNLogLevelName(logEvent.Level)
                .WithCustomProperty("logger_name", logEvent.LoggerName);

            if (!string.IsNullOrEmpty(Facility))
                messageBuilder.WithCustomProperty("facility", Facility);

            var properties = GetAllProperties(logEvent);
            foreach (var property in properties)
            {
                try
                {
                    if (!string.IsNullOrEmpty(property.Key))
                    {
                        if (Convert.GetTypeCode(property.Value) != TypeCode.Object)
                            messageBuilder.WithCustomProperty(property.Key, property.Value);
                        else
                            messageBuilder.WithCustomProperty(property.Key, property.Value?.ToString());
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    InternalLogger.Error(ex, "GraylogHttp(Name={0}): Fail to handle LogEvent Properties", Name);
                }
            }

            if (logEvent.Exception != null)
            {
                if (!string.IsNullOrEmpty(logEvent.Exception.Message))
                    messageBuilder.WithCustomProperty("_exception_message", logEvent.Exception.Message);
                if (!string.IsNullOrEmpty(logEvent.Exception.StackTrace))
                    messageBuilder.WithCustomProperty("_exception_stack_trace", logEvent.Exception.StackTrace);
            }

            try
            {
                _policy.Execute(() =>
                {
                    var content = new StringContent(messageBuilder.Render(logEvent.TimeStamp), Encoding.UTF8, "application/json");
                    return _httpClient.PostAsync(_requestAddress, content).Result.EnsureSuccessStatusCode();
                });
            }
            catch (BrokenCircuitException)
            {
                InternalLogger.Error(
                    "GraylogHttp(Name={0}): The Graylog server seems to be inaccessible, the log messages were not sent to the server.",
                    Name);
            }
        }

        /// <summary>
        /// Gets the machine name.
        /// </summary>
        private string GetMachineName()
        {
            return TryLookupValue(() => Environment.GetEnvironmentVariable("COMPUTERNAME"), "COMPUTERNAME")
                   ?? TryLookupValue(() => Environment.GetEnvironmentVariable("HOSTNAME"), "HOSTNAME")
                   ?? TryLookupValue(() => Dns.GetHostName(), "DnsHostName");
        }

        private string TryLookupValue(Func<string> lookupFunc, string lookupType)
        {
            try
            {
                string lookupValue = lookupFunc()?.Trim();
                return string.IsNullOrEmpty(lookupValue) ? null : lookupValue;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                InternalLogger.Warn(ex, "GraylogHttp(Name={0}): Failed to lookup {1}", Name, lookupType);
                return null;
            }
        }
    }
}
