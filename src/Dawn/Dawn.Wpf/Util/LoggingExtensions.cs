using Serilog;
using Serilog.Events;
using System;

namespace Dawn.Wpf
{
    public static class LoggingExtensions
    {
        public static void LogError(this ILogger log, Exception ex)
        {
            log.Write(LogEventLevel.Error, ex, "Unexpected Error occured");
        }
    }
}
