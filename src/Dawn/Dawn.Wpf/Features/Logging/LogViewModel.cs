using DynamicData;
using DynamicData.Binding;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using MvvmScarletToolkit;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace Dawn.Wpf
{
    public sealed class LogViewModel : ObservableObject, ILogEventSink, IDisposable
    {
        private readonly ObservableCollectionExtended<LogEventViewModel> _items;
        private readonly ObservableCollectionExtended<LogEventViewModel> _errors;
        private readonly SourceCache<LogEventViewModel, long> _sourceCache;
        private readonly IScarletCommandBuilder _commandBuilder;
        private readonly SynchronizationContext _context;
        private readonly DispatcherProgress<decimal> _dispatcherProgress;

        private IDisposable _subscription;
        private bool _disposedValue;
        private long _index;

        private int _precentage;
        public int Percentage
        {
            get { return _precentage; }
            private set { SetProperty(ref _precentage, value); }
        }

        public ReadOnlyObservableCollection<LogEventViewModel> Items { get; }

        public ReadOnlyObservableCollection<LogEventViewModel> Errors { get; }

        public IProgress<decimal> Progress => _dispatcherProgress;

        public LogViewModel(in IScarletCommandBuilder commandBuilder, SynchronizationContext context)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dispatcherProgress = new DispatcherProgress<decimal>(commandBuilder.Dispatcher, SetPercentage, TimeSpan.FromMilliseconds(250));

            _sourceCache = new SourceCache<LogEventViewModel, long>(vm => vm.Key);
            _items = new ObservableCollectionExtended<LogEventViewModel>();
            Items = new ReadOnlyObservableCollection<LogEventViewModel>(_items);

            _errors = new ObservableCollectionExtended<LogEventViewModel>();
            Errors = new ReadOnlyObservableCollection<LogEventViewModel>(_errors);

            _subscription = CreateSubscription(context);
        }

        private IDisposable CreateSubscription(SynchronizationContext context)
        {
            var sourceObservable = _sourceCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged();

            var importantEvents = sourceObservable
                .Filter(q => q.Level >= LogEventLevel.Information);

            var lessImportantEvents = sourceObservable
                .Filter(q => q.Level < LogEventLevel.Information)
                .Sample(TimeSpan.FromMilliseconds(15));

            var itemsSubscription = importantEvents
                .Merge(lessImportantEvents)
                .Sort(SortExpressionComparer<LogEventViewModel>.Descending(p => p.Timestamp), SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(context)
                .Bind(_items)
                .DisposeMany()
                .Subscribe();

            var criticalEvents = sourceObservable
                .Filter(q => q.Level > LogEventLevel.Warning);

            var errorsSubscription = criticalEvents
                .Sort(SortExpressionComparer<LogEventViewModel>.Descending(p => p.Timestamp), SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(context)
                .Bind(_errors)
                .DisposeMany()
                .Subscribe();

            return new CompositeDisposable(itemsSubscription, errorsSubscription);
        }

        public IDisposable Begin()
        {
            return new Subscription(this);
        }

        private void SetPercentage(decimal percentage)
        {
            var newValue = Convert.ToInt32(Math.Round(percentage, 0, MidpointRounding.AwayFromZero));

            _commandBuilder.Dispatcher.Invoke(() => Percentage = newValue);
        }

        public void Emit(LogEvent logEvent)
        {
            _sourceCache.AddOrUpdate(new LogEventViewModel(++_index, logEvent, this));
        }

        /// <summary>
        ///   we need to clear the bound collection, before the UI is bound to it for performance reasons
        /// </summary>
        public void PrepareBegin()
        {
            _sourceCache.Clear();
        }

        private void Setup()
        {
            _index = 0;
            Progress.Report(0);

            _subscription = CreateSubscription(_context);
        }

        private void Clear()
        {
            _subscription.Dispose();
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

        private sealed class Subscription : IDisposable
        {
            private readonly LogViewModel _viewModel;

            public Subscription(LogViewModel viewModel)
            {
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

                _viewModel.Setup();
            }

            public void Dispose()
            {
                _viewModel.Clear();
            }
        }
    }
}
