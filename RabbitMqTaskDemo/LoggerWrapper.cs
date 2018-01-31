using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RabbitMqTaskDemo
{
    public interface ILoggerWrapper
    {
        void Log(LongRunningTaskLogEntry logEntry);
        void Log(LongRunningTaskLogEntry logEntry, Exception exception);
        ConcurrentBag<LongRunningTaskLogEntry> InMemoryLogs { get; }
        int MaximumInMemoryLogCount { set; }
    }
    public class LoggerWrapper : ILoggerWrapper
    {
        public ConcurrentBag<LongRunningTaskLogEntry> InMemoryLogs { get; private set; } = new ConcurrentBag<LongRunningTaskLogEntry>();
        public int MaximumInMemoryLogCount { private get; set; }
        public string AppInsightsInstrumentationKey { get; set; } = String.Empty;
        private Logger logger = LogManager.GetLogger("UnconfiguredLogger");
        private TelemetryClient telemetry = new TelemetryClient();
        public LoggerWrapper(string logName)
        {
            logger = LogManager.GetLogger(logName);
        }

        public void Log(LongRunningTaskLogEntry logEntry)
        {
            Log(logEntry, null);
        }
        public void Log(LongRunningTaskLogEntry logEntry, Exception exception)
        {
            CheckInMemoryLogSizeAndTrucatePreservingExceptions();

            lock (logEntry)
            {
                InMemoryLogs.Add(logEntry);

                var formattedLogEntry = ConvertToJson(logEntry);

                if (logEntry.IsException)
                {
                    if (AppInsightsInstrumentationKey != String.Empty)
                    {
                        telemetry.InstrumentationKey = AppInsightsInstrumentationKey;
                        if (exception != null) telemetry.TrackException(exception);
                    }
                    logger.Error(formattedLogEntry);
                }
                logger.Info(formattedLogEntry);
            }
        }

        private void CheckInMemoryLogSizeAndTrucatePreservingExceptions()
        {
            if (InMemoryLogs.Count() > MaximumInMemoryLogCount)
            {
                InMemoryLogs = new ConcurrentBag<LongRunningTaskLogEntry>(InMemoryLogs.Where(e => e.IsException == true));
            }
        }

        private string ConvertToJson(object logEntry)
        {
            return JsonConvert.SerializeObject(logEntry);
        }


    }
}
