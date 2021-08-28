using DynamicData;
using DynamicData.Binding;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using MvvmScarletToolkit;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace Dawn.Wpf
{
    public sealed class LogViewModel : ObservableObject, ILogEventSink, IDisposable
    {
        private readonly SourceCache<LogEventViewModel, string> _sourceCache;
        private readonly IScarletCommandBuilder _commandBuilder;
        private readonly DispatcherProgress<decimal> _dispatcherProgress;
        private readonly IDisposable _subscription;

        private bool _disposedValue;

        private int _precentage;
        public int Percentage
        {
            get { return _precentage; }
            private set { SetProperty(ref _precentage, value); }
        }

        public IObservableCollection<LogEventViewModel> Items { get; }

        public IProgress<decimal> Progress => _dispatcherProgress;

        public LogViewModel(in IScarletCommandBuilder commandBuilder, SynchronizationContext context)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
            _dispatcherProgress = new DispatcherProgress<decimal>(commandBuilder.Dispatcher, SetPercentage, TimeSpan.FromMilliseconds(250));

            _sourceCache = new SourceCache<LogEventViewModel, string>(vm => vm.Key);
            Items = new ObservableCollectionExtended<LogEventViewModel>();

            _subscription = _sourceCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .Sample(TimeSpan.FromMilliseconds(15))
                .Sort(SortExpressionComparer<LogEventViewModel>.Descending(p => p.Timestamp), SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(context)
                .Bind(Items)
                .DisposeMany()
                .Subscribe();
        }

        private void SetPercentage(decimal percentage)
        {
            var newValue = Convert.ToInt32(Math.Round(percentage, 0, MidpointRounding.AwayFromZero));

            _commandBuilder.Dispatcher.Invoke(() => Percentage = newValue);
        }

        public void Emit(LogEvent logEvent)
        {
            _sourceCache.AddOrUpdate(new LogEventViewModel(logEvent, this));
        }

        public void Clear()
        {
            Progress.Report(0);
            _sourceCache.Clear();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
