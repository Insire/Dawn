using MvvmScarletToolkit;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Dawn.Wpf
{
    public sealed class GlobalCommandExceptionHandler : IScarletExceptionHandler
    {
        private readonly ILogger _log;

        public GlobalCommandExceptionHandler(ILogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task Handle(Exception ex)
        {
            _log.LogError(ex);

            return Task.CompletedTask;
        }
    }
}
