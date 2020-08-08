using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Serilog.Core;
using Serilog.Events;

namespace Dawn.Wpf
{
    public sealed class LogViewModel : ViewModelListBase<ViewModelContainer<LogEvent>>, ILogEventSink
    {
        public LogViewModel(in IScarletCommandBuilder commandBuilder)
            : base(commandBuilder)
        {
        }

        public void Emit(LogEvent logEvent)
        {
            Add(new ViewModelContainer<LogEvent>(logEvent));
        }
    }
}
