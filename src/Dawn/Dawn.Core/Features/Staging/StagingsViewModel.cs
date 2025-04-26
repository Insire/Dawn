using CommunityToolkit.Mvvm.Input;
using Dawn.Core.Features.Backups;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Serilog.Events;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Dawn.Core.Features.Staging
{
    /// <summary>
    ///     displays files that will be copied to a folder
    /// </summary>
    public sealed class StagingsViewModel : ViewModelBase
    {
        private readonly BackupsViewModel _backupsViewModel;
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly CompositeDisposable _disposables;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _log;
        private readonly LogViewModel _logViewModel;
        private readonly SourceCache<StagingViewModel, string> _sourceCache;


        private bool _reuseLastBackup;

        public bool ReuseLastBackup
        {
            get { return _reuseLastBackup; }
            set { SetProperty(ref _reuseLastBackup, value); }
        }

        private StagingViewModel? _selectedItem;

        [UsedImplicitly]
        public StagingViewModel? SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }

        private bool _isEmpty;

        public bool IsEmpty
        {
            get { return _isEmpty; }
            private set { SetProperty(ref _isEmpty, value); }
        }

        public ReadOnlyObservableCollection<StagingViewModel> Items { get; }

        public ICommand ApplyCommand { get; }
        public ICommand AddFilesCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand RemoveCommand { get; }

        public ICommand ClearCommand { get; }
        public Func<Task<bool>>? OnEmptyDirectoryCreated { get; set; }
        public Func<Task>? OnApplyingStagings { get; set; }

        public StagingsViewModel(
            IScarletCommandBuilder commandBuilder,
            ConfigurationViewModel configurationViewModel,
            ILogger log,
            LogViewModel logViewModel,
            BackupsViewModel backupsViewModel,
            IFileSystem fileSystem,
            SynchronizationContext context)
            : base(commandBuilder)
        {
            _configurationViewModel =
                configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _log = log?.ForContext<StagingsViewModel>() ?? throw new ArgumentNullException(nameof(log));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _backupsViewModel = backupsViewModel ?? throw new ArgumentNullException(nameof(backupsViewModel));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            _sourceCache = new SourceCache<StagingViewModel, string>(vm => vm.FullPath);
            var items = new ObservableCollectionExtended<StagingViewModel>();
            Items = new ReadOnlyObservableCollection<StagingViewModel>(items);

            var subscription1 = _sourceCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .Sort(SortExpressionComparer<StagingViewModel>.Ascending(p => p.FullPath),
                    SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(context)
                .Bind(items)
                .DisposeMany()
                .Subscribe();

            ApplyCommand = commandBuilder
                .Create(ApplyImpl, CanApply)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();

            AddFilesCommand = commandBuilder
                .Create(AddFilesImpl, CanAddFilesImpl)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();

            AddFolderCommand = commandBuilder
                .Create(AddFolderImpl, CanAddFolderImpl)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();

            RemoveCommand = new RelayCommand<object?>(RemoveImpl, CanRemoveImpl);
            ClearCommand = new RelayCommand(ClearImpl);
            IsEmpty = true;

            var subscription2 = Items
                .WhenPropertyChanged(p => p.Count, false)
                .ObserveOn(context)
                .Subscribe(p =>
                {
                    IsEmpty = p.Value == 0;
                });

            _disposables = new CompositeDisposable(subscription1, subscription2);
        }

        private void RemoveImpl(object? args)
        {
            if (args is StagingViewModel staging)
            {
                _sourceCache.Remove(staging);
            }
        }

        private static bool CanRemoveImpl(object? args)
        {
            return args is StagingViewModel;
        }

        private void ClearImpl()
        {
            _sourceCache.Clear();
        }

        private async Task AddFilesImpl()
        {
            var files = await _fileSystem.TrySelectFilesAsync();
            if (files is not null && files.Count > 0)
            {
                await Add(files).ConfigureAwait(false);
            }
        }

        private bool CanAddFilesImpl()
        {
            return !IsBusy
                   && !_configurationViewModel.HasErrors;
        }

        private async Task AddFolderImpl()
        {
            var folder = await _fileSystem.TrySelectFolderAsync();
            if (folder is not null)
            {
                await Add([folder]).ConfigureAwait(false);
            }
        }

        private bool CanAddFolderImpl()
        {
            return !IsBusy
                   && !_configurationViewModel.HasErrors;
        }

        public async Task Add(IReadOnlyList<string> fileSystemInfos)
        {
            var viewModels = new List<StagingViewModel>();
            foreach (var fileSystemInfo in fileSystemInfos)
            {
                if (_fileSystem.FileExists(fileSystemInfo))
                {
                    var viewModel = new StagingViewModel(fileSystemInfo);

                    viewModels.Add(viewModel);
                }

                if (_fileSystem.DirectoryExists(fileSystemInfo))
                {
                    var files = await Task
                        .Run(() => _fileSystem.GetFiles(fileSystemInfo, "*", SearchOption.AllDirectories))
                        .ConfigureAwait(false);

                    foreach (var file in files)
                    {
                        var viewModel = new StagingViewModel(file);

                        viewModels.Add(viewModel);
                    }
                }
            }

            _sourceCache.AddOrUpdate(viewModels);
        }

        private async Task ApplyImpl(CancellationToken token)
        {
            _logViewModel.PrepareBegin();

            var reuseLastBackup = ReuseLastBackup;
            var deploymentFolder = _configurationViewModel.DeploymentFolder;
            var backupFolder = _configurationViewModel.BackupFolder;
            var backupTypes = _configurationViewModel.BackupFileTypes.Items.Select(p => p.Extension).ToArray();
            var backupFileFolder = GetFolderName(_backupsViewModel.Items, backupFolder, reuseLastBackup);
            var now = DateTime.Now;

            _fileSystem.CreateDirectory(deploymentFolder);
            _fileSystem.CreateDirectory(backupFolder);
            _fileSystem.CreateDirectory(backupFileFolder);

            var t1 = OnApplyingStagings is null
                ? Task.CompletedTask
                : Dispatcher.Invoke(async () => await OnApplyingStagings.Invoke());

            var t2 = Task.Run(async () =>
            {
                try
                {
                    _logViewModel.Begin();

                    for (var i = 0; i < Items.Count; i++)
                    {
                        var newFile = Items[i];
                        _logViewModel.Progress.Report(i, Items.Count);

                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        var fileName = Path.GetFileName(newFile.FullPath);
                        var deploymentFileName = Path.Combine(deploymentFolder, fileName);
                        var backupFileName = Path.Combine(backupFileFolder, fileName);

                        if (backupTypes.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
                        {
                            BackupFile(newFile.FullPath, backupFileName, now, true);
                        }

                        Update(newFile.FullPath, deploymentFileName, now, _logViewModel.Progress);
                    }

                    if (_fileSystem.GetFiles(backupFileFolder, "*", SearchOption.TopDirectoryOnly).Length == 0)
                    {
                        var onEmptyDirectoryCreated = OnEmptyDirectoryCreated;
                        if (onEmptyDirectoryCreated is not null && await onEmptyDirectoryCreated.Invoke())
                        {
                            _fileSystem.DeleteDirectory(backupFileFolder, true);
                        }
                    }
                    else
                    {
                        _log.Write(LogEventLevel.Information, "Applied staged files to {FolderPath}", deploymentFolder);
                    }

                    _logViewModel.Complete();
                }
                catch (Exception ex)
                {
                    _log.LogError(ex);
                }
            }, token);

            await Task.WhenAll(t1, t2).ConfigureAwait(false);

            _sourceCache.Clear();

            await _backupsViewModel.Refresh(token).ConfigureAwait(false);
        }

        private static string GetFolderName(IEnumerable<BackupViewModel> backups, string rootFolder,
            bool reuseLastBackup)
        {
            if (!reuseLastBackup)
            {
                return Path.Combine(rootFolder, DateTime.Now.FormatAsBackup());
            }

            var reuseBackup = backups.OrderByDescending(p => p.TimeStamp).First();
            return Path.Combine(rootFolder, reuseBackup.TimeStamp.FormatAsBackup());
        }

        private bool CanApply()
        {
            return !IsBusy
                   && !_configurationViewModel.HasErrors;
        }

        private void Update(string from, string to, DateTime timeStamp, IProgress<decimal> progress)
        {
            var extension = Path.GetExtension(from).ToLowerInvariant();
            if (extension == ".zip")
            {
                CopyArchive(from, Path.GetDirectoryName(to)!, timeStamp, progress, true);
            }
            else if (Copy(from, to, timeStamp, true, _configurationViewModel.UpdateTimeStampOnApply))
            {
                _log.Write(LogEventLevel.Information, "Updated {File}", to);
            }
        }

        private void BackupFile(string from, string to, DateTime timeStamp, bool overwrite)
        {
            if (Copy(from, to, timeStamp, overwrite, _configurationViewModel.UpdateTimeStampOnApply))
            {
                _log.Write(LogEventLevel.Debug, "Created backup of {SourceFile} @ {BackupFile}", from, to);
            }
        }

        private void CopyArchive(string from, string to, DateTime timeStamp, IProgress<decimal> progress,
            bool overwrite)
        {
            if (_fileSystem.ExtractFor<StagingsViewModel>(from, to, _log, timeStamp, progress, overwrite,
                    _configurationViewModel.UpdateTimeStampOnApply))
            {
                _log.Write(LogEventLevel.Debug, "Extracted {BackupFile} to {SourceFile}", from, to);
            }
        }

        private bool Copy(string from, string to, DateTime timeStamp, bool overwrite, bool setLastWriteTime)
        {
            return _fileSystem.CopyFor<StagingsViewModel>(from, to, _log, timeStamp, overwrite, setLastWriteTime);
        }

        protected override void Dispose(bool disposing)
        {
            _disposables.Dispose();

            base.Dispose(disposing);
        }
    }
}
