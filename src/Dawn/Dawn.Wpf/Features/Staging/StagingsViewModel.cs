using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
{
    /// <summary>
    /// displays files that will be copied to a folder
    /// </summary>
    public sealed class StagingsViewModel : ViewModelBase
    {
        private readonly SourceCache<StagingViewModel, string> _sourceCache;
        private readonly ObservableCollectionExtended<StagingViewModel> _items;

        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly BackupsViewModel _backupsViewModel;
        private readonly IFileSystem _fileSystem;
        private readonly IDisposable _subscription;
        private readonly ILogger _log;

        private bool _reuseLastBackup;
        public bool ReuseLastBackup
        {
            get { return _reuseLastBackup; }
            set { SetProperty(ref _reuseLastBackup, value); }
        }

        private StagingViewModel _selectedItem;
        public StagingViewModel SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }

        public ReadOnlyObservableCollection<StagingViewModel> Items { get; }

        public ICommand ApplyCommand { get; }
        public ICommand AddFilesCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand RemoveCommand { get; }

        public ICommand ClearCommand { get; }
        public Func<bool> OnEmptyDirectoryCreated { get; set; }
        public Action OnApplyingStagings { get; set; }

        public StagingsViewModel(in IScarletCommandBuilder commandBuilder,
                                 ConfigurationViewModel configurationViewModel,
                                 ILogger log,
                                 LogViewModel logViewModel,
                                 BackupsViewModel backupsViewModel,
                                 IFileSystem fileSystem,
                                 SynchronizationContext context)
            : base(commandBuilder)
        {
            _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _log = log?.ForContext<StagingsViewModel>() ?? throw new ArgumentNullException(nameof(log));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _backupsViewModel = backupsViewModel ?? throw new ArgumentNullException(nameof(backupsViewModel));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            _sourceCache = new SourceCache<StagingViewModel, string>(vm => vm.FullPath);
            _items = new ObservableCollectionExtended<StagingViewModel>();
            Items = new ReadOnlyObservableCollection<StagingViewModel>(_items);

            _subscription = _sourceCache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .Sort(SortExpressionComparer<StagingViewModel>.Ascending(p => p.FullPath), SortOptimisations.ComparesImmutableValuesOnly)
                .ObserveOn(context)
                .Bind(_items)
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

            RemoveCommand = new RelayCommand<object>(RemoveImpl, CanRemoveImpl);
            ClearCommand = new RelayCommand(ClearImpl);
        }

        private void RemoveImpl(object args)
        {
            if (args is StagingViewModel staging)
            {
                _sourceCache.Remove(staging);
            }
        }

        private bool CanRemoveImpl(object args)
        {
            return args is StagingViewModel;
        }

        private void ClearImpl()
        {
            _sourceCache.Clear();
        }

        private async Task AddFilesImpl()
        {
            if (_fileSystem.TrySelectFiles(out var files))
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
            if (_fileSystem.TrySelectFolder(out var folder))
            {
                await Add(new[] { folder }).ConfigureAwait(false);
            }
        }

        private bool CanAddFolderImpl()
        {
            return !IsBusy
                && !_configurationViewModel.HasErrors;
        }

        public async Task Add(string[] fileSystemInfos)
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
                    var files = await Task.Run(() => _fileSystem.GetFiles(fileSystemInfo, "*", SearchOption.AllDirectories)).ConfigureAwait(false);

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

            var t1 = Dispatcher.Invoke(() => OnApplyingStagings?.Invoke());

            var t2 = Task.Run(() =>
           {
               try
               {
                   using (_logViewModel.Begin())
                   {
                       for (var i = 0; i < Items.Count; i++)
                       {
                           var newfile = Items[i];
                           _logViewModel.Progress.Report(i, Items.Count);

                           if (token.IsCancellationRequested)
                           {
                               return;
                           }

                           var fileName = Path.GetFileName(newfile.FullPath);
                           var deploymentFileName = Path.Combine(deploymentFolder, fileName);
                           var backupFileName = Path.Combine(backupFileFolder, fileName);

                           if (backupTypes.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
                           {
                               BackupFile(newfile.FullPath, backupFileName, now, true);
                           }

                           Update(newfile.FullPath, deploymentFileName, now, _logViewModel.Progress);
                       }

                       if (_fileSystem.GetFiles(backupFileFolder, "*", SearchOption.TopDirectoryOnly).Length == 0)
                       {
                           var delete = OnEmptyDirectoryCreated?.Invoke();
                           if (delete == true)
                           {
                               _fileSystem.DeleteDirectory(backupFileFolder, true);
                           }
                       }
                       else
                       {
                           _log.Write(Serilog.Events.LogEventLevel.Information, "Applied staged files to {FolderPath}", deploymentFolder);
                       }

                       _logViewModel.Progress.Report(100);
                   }
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

        private static string GetFolderName(IEnumerable<BackupViewModel> backups, string rootFolder, bool reuseLastBackup)
        {
            if (reuseLastBackup)
            {
                var reuseBackup = backups.OrderByDescending(p => p.TimeStamp).First();
                return Path.Combine(rootFolder, reuseBackup.TimeStamp.FormatAsBackup());
            }

            return Path.Combine(rootFolder, DateTime.Now.FormatAsBackup());
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
                CopyArchive(from, Path.GetDirectoryName(to), timeStamp, progress, true);
            }
            else if (Copy(from, to, timeStamp, true, _configurationViewModel.UpdateTimeStampOnApply))
            {
                _log.Write(Serilog.Events.LogEventLevel.Information, "Updated {File}", to);
            }
        }

        private void BackupFile(string from, string to, DateTime timeStamp, bool overwrite)
        {
            if (Copy(from, to, timeStamp, overwrite, _configurationViewModel.UpdateTimeStampOnApply))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Created backup of {SourceFile} @ {BackupFile}", from, to);
            }
        }

        private void CopyArchive(string from, string to, DateTime timeStamp, IProgress<decimal> progress, bool overwrite)
        {
            if (_fileSystem.ExtractFor<StagingsViewModel>(from, to, _log, timeStamp, progress, overwrite, _configurationViewModel.UpdateTimeStampOnApply))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Extracted {BackupFile} to {SourceFile}", from, to);
            }
        }

        private bool Copy(string from, string to, DateTime timeStamp, bool overwrite, bool setLastWriteTime)
        {
            return _fileSystem.CopyFor<StagingsViewModel>(from, to, _log, timeStamp, overwrite, setLastWriteTime);
        }

        protected override void Dispose(bool disposing)
        {
            _subscription?.Dispose();

            base.Dispose(disposing);
        }
    }
}
