using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Diagnostics;

namespace Dawn.Wpf
{
    public sealed class LogViewModel : ViewModelListBase<ViewModelContainer<LogEvent>>, ILogEventSink
    {
        private readonly DispatcherProgress<double> _dispatcherProgress;

        private int _precentage;
        public int Percentage
        {
            get { return _precentage; }
            private set { SetProperty(ref _precentage, value); }
        }

        public IProgress<double> Progress => _dispatcherProgress;

        public LogViewModel(in IScarletCommandBuilder commandBuilder)
            : base(commandBuilder)
        {
            _dispatcherProgress = new DispatcherProgress<double>(Dispatcher, SetPercentage, TimeSpan.FromMilliseconds(50));
        }

        private void SetPercentage(double percentage)
        {
            if (percentage == double.NaN)
            {
                return;
            }

            if (double.IsInfinity(percentage))
            {
                return;
            }

            if (double.IsNaN(percentage))
            {
                return;
            }

            var newValue = Convert.ToInt32(Math.Round(percentage, 0));
            Debug.WriteLine(newValue);
            Dispatcher.Invoke(() => Percentage = newValue);
        }

        public void Emit(LogEvent logEvent)
        {
            Add(new ViewModelContainer<LogEvent>(logEvent));
        }
    }
}
