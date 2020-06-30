using PaZword.Api;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;

namespace PaZword.Core
{
    [Export(typeof(ILogger))]
    [Shared()]
    internal sealed class Logger : ILogger
    {
        private const int LogSizelimit = 1000;

        private readonly IList<(string eventName, string description)> _logs = new List<(string eventName, string description)>();

        public event EventHandler LogsChanged;

        public StringBuilder GetAllLogs()
        {
            var builder = new StringBuilder();

            lock (_logs)
            {
                for (int i = 0; i < _logs.Count; i++)
                {
                    (string eventName, string description) = _logs[i];
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        builder.AppendLine(eventName);
                    }
                    else
                    {
                        builder.AppendLine($"{eventName} ; {description}");
                    }
                }
            }

            return builder;
        }

        public void LogEvent(string eventName, string description)
        {
            lock (_logs)
            {
                _logs.Add((eventName, description));
                EnsureLogsStaySmall();
                LogsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void LogFault(string eventName, string description, Exception exception)
        {
            lock (_logs)
            {
                var builder = new StringBuilder();
                builder.AppendLine(description);
                builder.AppendLine("| ------------------------------------------------------------");
                builder.AppendLine($"Exception message: {exception.Message}");
                builder.AppendLine($"Stack trace: {exception.StackTrace}");
                builder.AppendLine("| ------------------------------------------------------------");
                _logs.Add((eventName, builder.ToString()));
                EnsureLogsStaySmall();
                LogsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void EnsureLogsStaySmall()
        {
            lock (_logs)
            {
                // To be sure the logs don't consume too much memory, we keep them under a reasonable size.
                if (_logs.Count > LogSizelimit)
                {
                    for (int i = 0; i < _logs.Count - LogSizelimit; i++)
                    {
                        _logs.RemoveAt(0);
                    }
                }
            }
        }
    }
}
