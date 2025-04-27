using JetBrains.Annotations;
using System.Globalization;

namespace Dawn.Core.Features.Backups
{
    /// <summary>
    /// displays past updates
    /// </summary>
    public sealed class BackupsViewModel : BusinessViewModelListBase<BackupViewModel>
    {
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly BackupViewModelFactory _viewModelFactory;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _log;

        private bool _isMassDeleting;

        public ICommand RestoreCommand { [UsedImplicitly] get; }

        public ICommand DeleteAllCommand { get; }

        public Func<Task<bool>>? OnDeleteAllRequested { get; set; }
        public Func<Task<bool>>? OnDeleteRequested { get; set; }

        public Func<Task>? OnDeletingAll { get; set; }
        public Func<Task>? OnDeleting { get; set; }
        public Func<Task>? OnRestoring { get; set; }

        public Func<BackupViewModel, Task>? OnDetectChanges { get; set; }

        public Func<BackupViewModel, Task<BackupViewModel>>? OnMetaDataEditing { get; set; }

        public BackupsViewModel(
            in IScarletCommandBuilder commandBuilder,
            ConfigurationViewModel configurationViewModel,
            ILogger log,
            LogViewModel logViewModel,
            BackupViewModelFactory viewModelFactory,
            IFileSystem fileSystem)
            : base(commandBuilder)
        {
            _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _log = log?.ForContext<BackupsViewModel>() ?? throw new ArgumentNullException(nameof(log));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            RestoreCommand = commandBuilder
                .Create<BackupViewModel>(RestoreImpl, CanRestore)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();

            DeleteAllCommand = commandBuilder
                .Create(DeleteAllImpl, CanDeleteAllImpl)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();
        }

        protected override async Task RefreshInternal(CancellationToken token)
        {
            try
            {
                if (!_fileSystem.DirectoryExists(_configurationViewModel.BackupFolder))
                {
                    _log.Write(Serilog.Events.LogEventLevel.Warning, "Backup directory {FolderPath} does not exist. Aborting.", _configurationViewModel.BackupFolder);
                    return;
                }

                var lookup = new Dictionary<string, BackupViewModel>();
                var directories = await Task.Run(() => _fileSystem.GetDirectories(_configurationViewModel.BackupFolder, "*", SearchOption.TopDirectoryOnly), token).ConfigureAwait(false);

                foreach (var directory in directories)
                {
                    var key = Path.GetFileName(directory);
                    if (key.Length < 8)
                    {
                        continue;
                    }

                    try
                    {
                        if (!int.TryParse(key.AsSpan(0, 2), out var days))
                        {
                            days = 1;
                        }

                        if (!int.TryParse(key.AsSpan(2, 2), out var months))
                        {
                            months = 1;
                        }

                        if (!int.TryParse(key.AsSpan(4, 4), out var years))
                        {
                            years = 2001;
                        }

                        var hours = 0;
                        var minutes = 0;
                        var seconds = 0;

                        if (key.Length >= 10)
                        {
                            int.TryParse(key.AsSpan(8, 2), out hours);
                        }

                        if (key.Length >= 12)
                        {
                            int.TryParse(key.AsSpan(10, 2), out minutes);
                        }

                        if (key.Length >= 14)
                        {
                            int.TryParse(key.AsSpan(12, 2), out seconds);
                        }

                        days--;
                        months--;
                        years--;

                        var date = DateTime.MinValue
                            .AddDays(days)
                            .AddMonths(months)
                            .AddYears(years)
                            .AddHours(hours)
                            .AddMinutes(minutes)
                            .AddSeconds(seconds);

                        key = date.ToString("yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture);
                        if (!lookup.TryGetValue(key, out var value))
                        {
                            var model = new BackupModel() { FullPath = directory, Name = key, TimeStamp = date };
                            var group = _viewModelFactory.Get(model, this, OnDeleteRequestedImpl, OnDeletingImpl, OnMetaDataEditImpl, OnDetectChangesImpl);
                            var files = await Task.Run(() => _fileSystem.GetFiles(directory, "*", SearchOption.TopDirectoryOnly), token).ConfigureAwait(false);

                            await group.AddRange(files.Where(p => !p.EndsWith(IFileSystem.MetaDataFileName, StringComparison.InvariantCultureIgnoreCase)).Select(p => new FileInfoViewModel(p)), token).ConfigureAwait(false);

                            lookup.Add(key, group);
                        }
                        else
                        {
                            await value.Add(new DirectoryViewModel(directory), token).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex);
                    }
                }

                await AddRange(lookup.Values, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex);
            }
        }

        private async Task<bool> OnDeleteRequestedImpl()
        {
            if (_isMassDeleting)
            {
                return true;
            }

            var onDeleteRequested = OnDeleteRequested;
            if (onDeleteRequested is null)
            {
                return false;
            }

            return await onDeleteRequested();
        }

        private Task OnDeletingImpl()
        {
            if (!_isMassDeleting)
            {
                return OnDeleting is null
                    ? Task.CompletedTask
                    : OnDeleteRequested!.Invoke();
            }

            return Task.CompletedTask;
        }

        private Task<BackupViewModel> OnMetaDataEditImpl(BackupViewModel backup)
        {
            return OnMetaDataEditing!.Invoke(backup);
        }

        private Task OnDetectChangesImpl(BackupViewModel backup)
        {
            return OnDetectChanges is null
                ? Task.CompletedTask
                : OnDetectChanges(backup);
        }

        public override bool CanRefresh()
        {
            return !_configurationViewModel.HasErrors
                   && base.CanRefresh();
        }

        private async Task DeleteAllImpl(CancellationToken token)
        {
            var onDeleteAllRequested = OnDeleteAllRequested;
            if (onDeleteAllRequested is null)
            {
                return;
            }

            var shouldDeleteAll = await onDeleteAllRequested.Invoke();
            if (!shouldDeleteAll)
            {
                return;
            }

            _logViewModel.PrepareBegin();

            var t1 = Dispatcher.Invoke(() => OnDeletingAll?.Invoke());
            var t2 = Task.Run(async () =>
            {
                _logViewModel.Begin();
                _log.Write(Serilog.Events.LogEventLevel.Warning, "Deleting all backups in {FolderPath}", _configurationViewModel.BackupFolder);

                try
                {
                    _isMassDeleting = true;

                    for (var i = Items.Count - 1; i >= 0; i--)
                    {
                        _logViewModel.Progress.Report(i, Items.Count);

                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        var item = Items[i];
                        await item.Delete().ConfigureAwait(false);
                    }

                    await Refresh(token).ConfigureAwait(false);
                }
                finally
                {
                    _isMassDeleting = false;
                }

                _log.Write(Serilog.Events.LogEventLevel.Information, "Deleted all backups in {FolderPath}", _configurationViewModel.BackupFolder);
                _logViewModel.Complete();
            }, token);

            await Task.WhenAll(t1, t2).ConfigureAwait(false);
        }

        private bool CanDeleteAllImpl()
        {
            return !IsBusy
                   && !_configurationViewModel.HasErrors
                   && Items.Count > 0;
        }

        private async Task RestoreImpl(BackupViewModel backupViewModel, CancellationToken token)
        {
            var deploymentFolder = _configurationViewModel.DeploymentFolder;
            var backupFolder = _configurationViewModel.BackupFolder;

            if (!_fileSystem.DirectoryExists(deploymentFolder))
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, "Deployment folder {FolderPath} does not exist", deploymentFolder);

                return;
            }

            if (!_fileSystem.DirectoryExists(deploymentFolder))
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, "Backup folder {FolderPath} does not exist", backupFolder);

                return;
            }

            _logViewModel.PrepareBegin();

            var now = DateTime.Now;
            var t1 = Dispatcher.Invoke(() => OnRestoring?.Invoke());
            var t2 = Task.Run(() =>
            {
                _logViewModel.Begin();
                _log.Write(Serilog.Events.LogEventLevel.Information, "Restoring backup {BackupName}", backupViewModel.Name);

                var array = backupViewModel.Items.ToArray();
                for (var i = 0; i < array.Length; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    _logViewModel.Progress.Report(i, array.Length);

                    var file = array[i];
                    var extension = Path.GetExtension(file.FullPath).ToLowerInvariant();
                    if (extension == ".zip")
                    {
                        RestoreArchive(file.FullPath, deploymentFolder, now, null);
                    }
                    else
                    {
                        var fileName = Path.GetFileName(file.FullPath);
                        var restoreFileName = Path.Combine(deploymentFolder, fileName);

                        RestoreFile(file.FullPath, restoreFileName, now);
                    }
                }

                _log.Write(Serilog.Events.LogEventLevel.Information, "Restored backup {BackupName}", backupViewModel.Name);
                _logViewModel.Complete();
            }, token);

            await Task.WhenAll(t1, t2).ConfigureAwait(false);
        }

        private void RestoreFile(string from, string to, DateTime timeStamp)
        {
            if (_fileSystem.CopyFor<BackupsViewModel>(from, to, _log, timeStamp, true, _configurationViewModel.UpdateTimeStampOnRestore))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Restored backup of {SourcePath} from {DestinationPath}", to, from);
            }
        }

        private void RestoreArchive(string from, string to, DateTime timeStamp, IProgress<decimal>? progress)
        {
            if (_fileSystem.ExtractFor<BackupsViewModel>(from, to, _log, timeStamp, progress, true, _configurationViewModel.UpdateTimeStampOnRestore))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Restored backup of {SourcePath} from {DestinationPath}", to, from);
            }
        }

        private bool CanRestore(BackupViewModel? backupViewModel)
        {
            return !IsBusy
                   && backupViewModel != null
                   && !_configurationViewModel.HasErrors;
        }
    }
}
