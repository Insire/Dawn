using DynamicData;
using DynamicData.Binding;
using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public sealed class ChangeDetectionViewModel : ViewModelBase
    {
        private readonly SourceCache<FilePairViewModel, string> _sourceCache;
        private readonly ObservableCollectionExtended<FilePairViewModel> _items;
        private readonly ChangeDetectionService _service;

        public ReadOnlyObservableCollection<FilePairViewModel> Items { get; }

        private FilePairViewModel _selectedItem;
        public FilePairViewModel SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }

        public ICommand DetectChangesCommand { get; }

        public ChangeDetectionViewModel(IScarletCommandBuilder commandBuilder, SynchronizationContext context, ChangeDetectionService service)
            : base(commandBuilder)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            _sourceCache = new SourceCache<FilePairViewModel, string>(vm => vm.Source.FullPath);
            _items = new ObservableCollectionExtended<FilePairViewModel>();
            Items = new ReadOnlyObservableCollection<FilePairViewModel>(_items);

            _sourceCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .Sort(SortExpressionComparer<FilePairViewModel>.Ascending(p => p.ChangeState), SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(context)
                .Bind(_items)
                .DisposeMany()
                .Subscribe(changes =>
                {
                    if (_selectedItem is null)
                    {
                        foreach (var change in changes)
                        {
                            if (_selectedItem is not null)
                            {
                                break;
                            }

                            SelectedItem = change.Current;
                        }
                    }
                });

            DetectChangesCommand = commandBuilder
                .Create<object>(DetectChangesImpl, CanDetectChangesImpl)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();
        }

        public async Task DetectChanges(BackupViewModel backup)
        {
            var changes = await _service.DetectChanges(backup).ConfigureAwait(false);

            _sourceCache.RemoveKeys(changes.Select(p => p.Source.FullPath).Except(_sourceCache.Keys));
            _sourceCache.AddOrUpdate(changes);
        }

        private Task DetectChangesImpl(object arg)
        {
            if (arg is BackupViewModel backup)
            {
                return DetectChanges(backup);
            }

            return Task.CompletedTask;
        }

        private bool CanDetectChangesImpl(object arg)
        {
            return !IsBusy && arg is BackupViewModel;
        }
    }
}
