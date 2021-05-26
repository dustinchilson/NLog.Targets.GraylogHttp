using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NLog.Common;
using NLog.Config;
using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;

namespace NLog.Targets.GraylogHttp
{
    [Target("GraylogHttp")]
    public class GraylogHttpTarget : TargetWithContext
    {
        private HttpClient _httpClient;
        private Uri _requestAddress;
        private PolicyWrap _policy;

        public GraylogHttpTarget()
        {
            ContextProperties = new List<TargetPropertyWithContext>();
        }

        public GraylogHttpTarget(string name)
            : this()
        {
            Name = name;
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

        /// <summary>
        /// Send the NLog log level name as a custom field (default true).
        /// </summary>
        public bool AddNLogLevelName { get; set; } = true;

#if NETSTANDARD2_0
        /// <summary>
        /// Use IHttpClientFactory (loaded with <see cref="ConfigurationItemFactory.Default.CreateInstance"/>, default false).
        /// </summary>
        public bool UseHttpClientFactory { get; set; } = false;

        /// <summary>
        /// A named HttpClient from to be used with IHttpClientFactory.
        /// </summary>
        public string HttpClientName { get; set; }
#endif

        [ArrayParameter(typeof(TargetPropertyWithContext), "parameter")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "NLog Behavior")]
        public override IList<TargetPropertyWithContext> ContextProperties { get; }

        protected override void InitializeTarget()
        {
            if (string.IsNullOrEmpty(Host))
                Host = GetMachineName();

            _requestAddress = new Uri(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}/gelf", GraylogServer));

            if (!string.IsNullOrEmpty(GraylogPort))
                _requestAddress = new Uri(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}/gelf", GraylogServer, GraylogPort));

            _httpClient.DefaultRequestHeaders.ExpectContinue = false; // Expect (100) Continue breaks the graylog server

            var waitAndRetryPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException))
                .WaitAndRetry(
                    1,
                    attempt => TimeSpan.FromMilliseconds(200),
                    (exception, calculatedWaitDuration) =>
                    {
                        InternalLogger.Error(exception, "GraylogHttp(Name={0}): Graylog server error, the log messages were not sent to the server.", Name);
                    });

            var circuitBreakerPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException))
                .AdvancedCircuitBreakerAsync(
                    (double)FailureThreshold / 100,
                    TimeSpan.FromSeconds(SamplingDurationSeconds),
                    MinimumThroughput,
                    TimeSpan.FromSeconds(FailureCooldownSeconds),
                    (exception, span) => InternalLogger.Error(exception, "GraylogHttp(Name={0}): Circuit breaker Open", Name),
                    () => InternalLogger.Info("GraylogHttp(Name={0}): Circuit breaker Reset", Name));

            _policy = Policy.Wrap(waitAndRetryPolicy, circuitBreakerPolicy);

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

#if NETSTANDARD2_0
            // Filter out internal logs from own HttpClient
            if (UseHttpClientFactory
                && logEvent.LoggerName.StartsWith("System.Net.Http.HttpClient." + HttpClientName))
                return;
#endif

            GraylogMessageBuilder messageBuilder = new GraylogMessageBuilder()
                .WithProperty("short_message", logEvent.FormattedMessage)
                .WithProperty("host", Host)
                .WithLevel(logEvent.Level)
                .WithSyslogLevelName(logEvent.Level)
                .WithCustomProperty("logger_name", logEvent.LoggerName);

            if (AddNLogLevelName)
            {
                messageBuilder.WithNLogLevelName(logEvent.Level);
            }

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

            if (_httpClient == null)
                InitHttpClient();

            try
            {
                _policy.ExecuteAndCapture(() =>
                _policy.ExecuteAsync(async () =>
                {
                    using var content = new StringContent(messageBuilder.Render(logEvent.TimeStamp), Encoding.UTF8, "application/json");
                    var postTask = _httpClient.PostAsync(_requestAddress, content);
                    var result = await postTask.ConfigureAwait(false);
                    result.EnsureSuccessStatusCode();
                    return result;
                });
            }
            catch (BrokenCircuitException)
            {
                InternalLogger.Error(
                    "GraylogHttp(Name={0}): The Graylog server seems to be inaccessible, the log messages were not sent to the server.",
                    Name);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                InternalLogger.Error(
                    ex,
                    "GraylogHttp(Name={0}): Graylog server error, the log messages were not sent to the server.",
                    Name);
            }
        }

        private void InitHttpClient()
        {
#if NETSTANDARD2_0
            if (UseHttpClientFactory)
            {
                try
                {
                    var factoryInstance = ConfigurationItemFactory.Default.CreateInstance(typeof(System.Net.Http.IHttpClientFactory));
                    if (factoryInstance is IHttpClientFactory factory)
                        _httpClient = HttpClientName == null ? factory.CreateClient() : factory.CreateClient(HttpClientName);
                }
                catch (NLogConfigurationException ex)
                {
                    InternalLogger.Error(ex, "GraylogHttp(Name={0}): IHttpClientFactory couldn't be obtained.", Name);
                }
            }
#endif
            // Fallback to regular HttpClient
            if (_httpClient == null)
                _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.ExpectContinue = false; // Expect (100) Continue breaks the graylog server
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
