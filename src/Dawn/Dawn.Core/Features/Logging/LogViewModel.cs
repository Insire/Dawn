using DynamicData;
using DynamicData.Binding;
using Serilog.Core;
using Serilog.Events;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Dawn.Core.Features.Logging
{
    public sealed class LogViewModel : ObservableObject, ILogEventSink, IDisposable
    {
        private readonly IScarletCommandBuilder _commandBuilder;
        private readonly IClipboardService _clipboardService;
        private readonly DispatcherProgress<decimal> _dispatcherProgress;

        private readonly SourceCache<LogEventViewModel, long> _sourceCache;
        private readonly IDisposable _subscription;

        private bool _disposedValue;
        private long _index;

        private int _percentage;
        public int Percentage
        {
            get { return _percentage; }
            private set { SetProperty(ref _percentage, value); }
        }

        private int _total;
        public int Total
        {
            get { return _total; }
            private set { SetProperty(ref _total, value); }
        }

        private LogEventViewModel? _currentInfo;

        public LogEventViewModel? CurrentInfo
        {
            get { return _currentInfo; }
            private set { SetProperty(ref _currentInfo, value); }
        }

        private LogEventViewModel? _currentError;

        public LogEventViewModel? CurrentError
        {
            get { return _currentError; }
            private set { SetProperty(ref _currentError, value); }
        }

        public ReadOnlyObservableCollection<LogEventViewModel> Items { get; }
        public ReadOnlyObservableCollection<LogEventViewModel> Errors { get; }
        public IProgress<decimal> Progress => _dispatcherProgress;

        public LogViewModel(in IScarletCommandBuilder commandBuilder, SynchronizationContext context, IClipboardService clipboardService)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
            _clipboardService = clipboardService;
            _dispatcherProgress = new DispatcherProgress<decimal>(commandBuilder.Dispatcher, SetPercentage, TimeSpan.FromMilliseconds(250));

            var items = new ObservableCollectionExtended<LogEventViewModel>();
            Items = new ReadOnlyObservableCollection<LogEventViewModel>(items);

            var errors = new ObservableCollectionExtended<LogEventViewModel>();
            Errors = new ReadOnlyObservableCollection<LogEventViewModel>(errors);

            _sourceCache = new SourceCache<LogEventViewModel, long>(vm => vm.Key);
            var comparer = SortExpressionComparer<LogEventViewModel>.Descending(p => p.Timestamp);

            var countSubscription = _sourceCache.CountChanged
                .ObserveOn(TaskPoolScheduler.Default)
                .Throttle(TimeSpan.FromMilliseconds(250))
                .ObserveOn(context)
                .Subscribe(p => Total = p);

            var sourceObservable = _sourceCache
                .Connect()
                .Sort(comparer, SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(TaskPoolScheduler.Default);

            var itemsSubscription = sourceObservable
                .Filter(q => q.Level >= LogEventLevel.Information)
                .Merge(sourceObservable
                        .Filter(q => q.Level < LogEventLevel.Information))
                .Sort(comparer, SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(context)
                .Bind(items)
                .Batch(TimeSpan.FromMilliseconds(500))
                .DisposeMany()
                .Subscribe(changes =>
                {
                    var changedLogEvent = default(LogEventViewModel);
                    foreach (var change in changes)
                    {
                        if (change.Reason == ChangeReason.Add)
                        {
                            changedLogEvent = change.Current;
                        }
                    }

                    if (changedLogEvent is null)
                    {
                        return;
                    }

                    CurrentInfo = changedLogEvent;
                    changedLogEvent.RenderCommand.Execute(null);
                });

            var errorsSubscription = sourceObservable
                .Filter(q => q.Level > LogEventLevel.Warning)
                .Sort(comparer, SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(context)
                .Bind(errors)
                .DisposeMany()
                .Subscribe(changes =>
                {
                    var changedLogEvent = default(LogEventViewModel);
                    foreach (var change in changes)
                    {
                        if (change.Reason == ChangeReason.Add)
                        {
                            changedLogEvent = change.Current;
                        }
                    }

                    if (changedLogEvent is null)
                    {
                        return;
                    }

                    CurrentError = changedLogEvent;
                    changedLogEvent.RenderCommand.Execute(null);
                });

            _subscription = new CompositeDisposable(itemsSubscription, errorsSubscription, countSubscription, _sourceCache);
        }

        private void SetPercentage(decimal percentage)
        {
            var newValue = Convert.ToInt32(Math.Round(percentage, 0, MidpointRounding.AwayFromZero));

            _commandBuilder.Dispatcher.Invoke(() => Percentage = newValue);
        }

        public void Emit(LogEvent logEvent)
        {
            _sourceCache?.AddOrUpdate(new LogEventViewModel(++_index, logEvent, this, _clipboardService));
        }

        /// <summary>
        /// we need to clear the bound collection, before the UI is bound to it for performance reasons
        /// </summary>
        public void PrepareBegin()
        {
            _sourceCache.Clear();
        }

        public void Setup()
        {
            _index = 0;
            Progress.Report(0);
        }

        private void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                _subscription?.Dispose();
            }

            _disposedValue = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }
    }
}
