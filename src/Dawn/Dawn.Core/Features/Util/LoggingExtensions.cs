using Serilog.Events;

namespace Dawn.Core.Features.Util
{
    public static class LoggingExtensions
    {
        public static void LogError(this ILogger log, Exception ex)
        {
            log.Write(LogEventLevel.Error, ex, "Unexpected Error occured");
        }
    }
}
