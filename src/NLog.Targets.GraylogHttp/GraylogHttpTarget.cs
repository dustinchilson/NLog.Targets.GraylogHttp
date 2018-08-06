using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using NLog.Common;
using NLog.Config;

namespace NLog.Targets.GraylogHttp
{
    [Target("GraylogHttp")]
    public class GraylogHttpTarget : TargetWithContext
    {
        public GraylogHttpTarget()
        {
            ContextProperties = new List<TargetPropertyWithContext>();
        }

        [RequiredParameter]
        public string GraylogServer { get; set; }

        [RequiredParameter]
        public string GraylogPort { get; set; }

        [RequiredParameter]
        public string Facility { get; set; }

        public string Host { get; set; }

        [ArrayParameter(typeof(TargetPropertyWithContext), "parameter")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "NLog Behavior")]
        public override IList<TargetPropertyWithContext> ContextProperties { get; }

        private HttpClient _httpClient;

        private Uri _requestAddress;

        protected override void InitializeTarget()
        {
            if (string.IsNullOrEmpty(Host))
                Host = GetMachineName();

            _httpClient = new HttpClient();
            _requestAddress = new Uri(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}/gelf", GraylogServer, GraylogPort));
            _httpClient.DefaultRequestHeaders.ExpectContinue = false; // Expect (100) Continue breaks the graylog server

            // Prefix the custom properties with underscore upfront, so we dont have to do it for each logevent
            for (int i = 0; i < ContextProperties.Count; ++i)
            {
                var p = ContextProperties[i];
                if (!p.Name.StartsWith("_", StringComparison.Ordinal))
                    p.Name = string.Concat("_", p.Name);
            }

            base.InitializeTarget();
        }

        protected override void Write(LogEventInfo logEvent)
        {
            GraylogMessageBuilder messageBuilder = new GraylogMessageBuilder()
                .WithCustomProperty("_facility", Facility)
                .WithProperty("short_message", logEvent.FormattedMessage)
                .WithProperty("host", Host)
                .WithLevel(logEvent.Level)
                .WithCustomProperty("_logger_name", logEvent.LoggerName);

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
                catch (Exception ex)
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
                // Ensure to reuse the same HttpClient. See also: https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
                _httpClient.PostAsync(_requestAddress, new StringContent(messageBuilder.Render(logEvent.TimeStamp), Encoding.UTF8, "application/json")).Result.EnsureSuccessStatusCode();
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.Flatten().InnerExceptions)
                    InternalLogger.Error(inner, "GraylogHttp(Name={0}): Fail to post LogEvents", Name);
            }
            catch (Exception ex)
            {
                InternalLogger.Error(ex, "GraylogHttp(Name={0}): Fail to post LogEvents", Name);
            }
        }

        /// <summary>
        /// Gets the machine name
        /// </summary>
        private static string GetMachineName()
        {
            return TryLookupValue(() => Environment.GetEnvironmentVariable("COMPUTERNAME"), "COMPUTERNAME")
                ?? TryLookupValue(() => Environment.GetEnvironmentVariable("HOSTNAME"), "HOSTNAME")
                ?? TryLookupValue(() => Dns.GetHostName(), "DnsHostName");
        }

        private static string TryLookupValue(Func<string> lookupFunc, string lookupType)
        {
            try
            {
                string lookupValue = lookupFunc()?.Trim();
                return string.IsNullOrEmpty(lookupValue) ? null : lookupValue;
            }
            catch (Exception ex)
            {
                InternalLogger.Warn(ex, "GraylogHttp: Failed to lookup {0}", lookupType);
                return null;
            }
        }
    }
}
