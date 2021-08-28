using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
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

        public ICommand RestoreCommand { get; }

        public ICommand DeleteAllCommand { get; }

        public Func<bool> OnDeleteAllRequested { get; set; }
        public Func<bool> OnDeleteRequested { get; set; }

        public Action OnDeletingAll { get; set; }

        public Action OnDeleting { get; set; }

        public Action OnRestoring { get; set; }

        public Func<BackupViewModel, BackupViewModel> OnMetaDataEditing { get; set; }

        public BackupsViewModel(in IScarletCommandBuilder commandBuilder,
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
                .Create<BackupViewModel>(Restore, CanRestore)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();

            DeleteAllCommand = commandBuilder
                .Create(DeleteAll, CanDeleteAll)
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
                var directories = await Task.Run(() => _fileSystem.GetDirectories(_configurationViewModel.BackupFolder, "*", SearchOption.TopDirectoryOnly)).ConfigureAwait(false);

                foreach (var directory in directories)
                {
                    var key = Path.GetFileName(directory);
                    if (key.Length >= 8)
                    {
                        try
                        {
                            if (!int.TryParse(key.Substring(0, 2), out var days))
                            {
                                days = 1;
                            }

                            if (!int.TryParse(key.Substring(2, 2), out var months))
                            {
                                months = 1;
                            }

                            if (!int.TryParse(key.Substring(4, 4), out var years))
                            {
                                years = 2001;
                            }

                            var hours = 0;
                            var minutes = 0;
                            var seconds = 0;

                            if (key.Length >= 10)
                            {
                                int.TryParse(key.Substring(8, 2), out hours);
                            }

                            if (key.Length >= 12)
                            {
                                int.TryParse(key.Substring(10, 2), out minutes);
                            }

                            if (key.Length >= 14)
                            {
                                int.TryParse(key.Substring(12, 2), out seconds);
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
                            if (!lookup.ContainsKey(key))
                            {
                                var group = _viewModelFactory.Get(new BackupModel()
                                {
                                    FullPath = directory,
                                    Name = key,
                                    TimeStamp = date
                                }, this, () => _isMassDeleting || (OnDeleteRequested?.Invoke() ?? false), () => { if (!_isMassDeleting) { OnDeleting?.Invoke(); } }, (vm) => OnMetaDataEditing?.Invoke(vm));
                                var files = await Task.Run(() => _fileSystem.GetFiles(directory, "*", SearchOption.TopDirectoryOnly)).ConfigureAwait(false);

                                await group.AddRange(files.Where(p => !p.EndsWith("backup.json", StringComparison.InvariantCultureIgnoreCase)).Select(p => new ViewModelContainer<string>(p)), token).ConfigureAwait(false);

                                lookup.Add(key, group);
                            }
                            else
                            {
                                await lookup[key].Add(new ViewModelContainer<string>(directory), token).ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex);
                        }
                    }
                }

                await AddRange(lookup.Values, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex);
            }
        }

        public override bool CanRefresh()
        {
            return !_configurationViewModel.HasErrors
                && base.CanRefresh();
        }

        private async Task DeleteAll(CancellationToken token)
        {
            try
            {
                var shouldDeleteAll = OnDeleteAllRequested?.Invoke() ?? false;

                if (!shouldDeleteAll)
                {
                    return;
                }

                _logViewModel.PrepareBegin();

                var t1 = Dispatcher.Invoke(() => OnDeletingAll?.Invoke());

                var t2 = Task.Run(async () =>
                {
                    using (_logViewModel.Begin())
                    {
                        _log.Write(Serilog.Events.LogEventLevel.Warning, "Deleting all backups in {FolderPath}", _configurationViewModel.BackupFolder);

                        try
                        {
                            _isMassDeleting = true;

                            for (var i = 0; i < Items.Count; i++)
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

                        _logViewModel.Progress.Report(100);
                        _log.Write(Serilog.Events.LogEventLevel.Information, "Deleted all backups in {FolderPath}", _configurationViewModel.BackupFolder);
                    }
                }, token);

                await Task.WhenAll(t1, t2).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex);
            }
        }

        private bool CanDeleteAll()
        {
            return !IsBusy
                && !_configurationViewModel.HasErrors
                && Items.Count > 0;
        }

        private async Task Restore(BackupViewModel backupViewModel, CancellationToken token)
        {
            try
            {
                _logViewModel.PrepareBegin();

                var deploymentFolder = _configurationViewModel.DeploymentFolder;
                var backupFolder = _configurationViewModel.BackupFolder;
                var now = DateTime.Now;

                _log.Write(Serilog.Events.LogEventLevel.Information, "Restoring backup {BackupName}", backupViewModel.Name);

                var t1 = Dispatcher.Invoke(() => OnRestoring?.Invoke());

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

                var t2 = Task.Run(() =>
                {
                    using (_logViewModel.Begin())
                    {
                        var array = backupViewModel.Items.ToArray();
                        for (var i = 0; i < array.Length; i++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            var percentage = i * 100m / (array.Length - 1);
                            _logViewModel.Progress.Report(percentage);

                            var file = array[i];
                            var extension = Path.GetExtension(file.Value).ToLowerInvariant();
                            if (extension == ".zip")
                            {
                                RestoreArchive(file.Value, deploymentFolder, now, null);
                            }
                            else
                            {
                                var fileName = Path.GetFileName(file.Value);
                                var restoreFileName = Path.Combine(deploymentFolder, fileName);

                                RestoreFile(file.Value, restoreFileName, now);
                            }
                        }

                        _logViewModel.Progress.Report(100);
                        _log.Write(Serilog.Events.LogEventLevel.Information, "Restored backup {BackupName}", backupViewModel.Name);
                    }
                }, token);

                await Task.WhenAll(t1, t2).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex);
            }
        }

        private void RestoreFile(string from, string to, DateTime timeStamp)
        {
            if (_fileSystem.CopyFor<BackupsViewModel>(from, to, _log, timeStamp, true, _configurationViewModel.UpdateTimeStampOnRestore))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Restored backup of {SourcePath} from {DestinationPath}", to, from);
            }
        }

        private void RestoreArchive(string from, string to, DateTime timeStamp, IProgress<decimal> progress)
        {
            if (_fileSystem.ExtractFor<BackupsViewModel>(from, to, _log, timeStamp, progress, true, _configurationViewModel.UpdateTimeStampOnRestore))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Restored backup of {SourcePath} from {DestinationPath}", to, from);
            }
        }

        private bool CanRestore(BackupViewModel backupViewModel)
        {
            return !IsBusy
                && backupViewModel != null
                && !_configurationViewModel.HasErrors;
        }
    }
}
