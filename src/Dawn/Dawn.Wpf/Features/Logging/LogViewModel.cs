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

        private readonly IScarletCommandBuilder _commandBuilder;
        private readonly DispatcherProgress<decimal> _dispatcherProgress;

        private readonly SourceCache<LogEventViewModel, long> _sourceCache;
        private readonly IDisposable _subscription;

        private bool _disposedValue;
        private long _index;

        private int _precentage;
        public int Percentage
        {
            get { return _precentage; }
            private set { SetProperty(ref _precentage, value); }
        }

        private int _total;
        public int Total
        {
            get { return _total; }
            private set { SetProperty(ref _total, value); }
        }

        public ReadOnlyObservableCollection<LogEventViewModel> Items { get; }
        public ReadOnlyObservableCollection<LogEventViewModel> Errors { get; }
        public IProgress<decimal> Progress => _dispatcherProgress;

        public LogViewModel(in IScarletCommandBuilder commandBuilder, SynchronizationContext context)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
            _dispatcherProgress = new DispatcherProgress<decimal>(commandBuilder.Dispatcher, SetPercentage, TimeSpan.FromMilliseconds(250));

            _items = new ObservableCollectionExtended<LogEventViewModel>();
            Items = new ReadOnlyObservableCollection<LogEventViewModel>(_items);

            _errors = new ObservableCollectionExtended<LogEventViewModel>();
            Errors = new ReadOnlyObservableCollection<LogEventViewModel>(_errors);

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
                .LimitSizeTo(50)
                .Sort(comparer, SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(context)
                .Bind(_items)
                .DisposeMany()
                .Subscribe();

            var errorsSubscription = sourceObservable
                .Filter(q => q.Level > LogEventLevel.Warning)
                .Sort(comparer, SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(context)
                .Bind(_errors)
                .DisposeMany()
                .Subscribe();

            _subscription = new CompositeDisposable(itemsSubscription, errorsSubscription, countSubscription, _sourceCache);
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
            _sourceCache?.AddOrUpdate(new LogEventViewModel(++_index, logEvent, this));
        }

        /// <summary>
        /// we need to clear the bound collection, before the UI is bound to it for performance reasons
        /// </summary>
        public void PrepareBegin()
        {
            _sourceCache.Clear();
        }

        private void Setup()
        {
            _index = 0;
            Progress.Report(0);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription?.Dispose();
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
            }
        }
    }
}
