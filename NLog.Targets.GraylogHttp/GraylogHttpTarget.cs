using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NLog.Config;

namespace NLog.Targets.GraylogHttp
{
    [Target("GraylogHttp")]
    public class GraylogHttpTarget : TargetWithLayout
    {
        public GraylogHttpTarget()
        {
            this.Host = Environment.MachineName;
            this.Parameters = new List<GraylogParameterInfo>();
        }

        [RequiredParameter]
        public string GraylogServer { get; set; }

        [RequiredParameter]
        public string GraylogPort { get; set; }

        [RequiredParameter]
        public string Facility { get; set; }

        public string Host { get; set; }

        [ArrayParameter(typeof(GraylogParameterInfo), "parameter")]
        public IList<GraylogParameterInfo> Parameters { get; private set; }

        protected override void Write(LogEventInfo logEvent)
        {
            GraylogMessageBuilder messageBuilder = new GraylogMessageBuilder()
                .WithCustomProperty("facility", this.Facility)
                .WithProperty("short_message", logEvent.Message)
                .WithProperty("host", this.Host)
                .WithLevel(logEvent.Level)
                .WithCustomProperty("logger_name", logEvent.LoggerName);

            if (this.Parameters != null && this.Parameters.Any())
            {
                Dictionary<string, string> paramsDictionary = this.Parameters
                    .Select(p => new KeyValuePair<string, string>(p.Name, p.Layout.Render(logEvent)))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);

                messageBuilder.WithCustomPropertyRange(paramsDictionary);
            }

            if (logEvent.Exception != null)
            {
                if (!string.IsNullOrEmpty(logEvent.Exception.Message))
                    messageBuilder.WithCustomProperty("exception_message", logEvent.Exception.Message);
                if (!string.IsNullOrEmpty(logEvent.Exception.StackTrace))
                    messageBuilder.WithCustomProperty("exception_stack_trace", logEvent.Exception.StackTrace);
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(string.Format("{0}:{1}/gelf", this.GraylogServer, this.GraylogPort));
                httpClient.DefaultRequestHeaders.ExpectContinue = false; // Expect (100) Continue breaks the graylog server
                HttpResponseMessage httpResponseMessage = httpClient.PostAsync("", new StringContent(messageBuilder.Render(), Encoding.UTF8, "application/json")).Result;
            }
        }
    }
}
