using ImTools;
using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
{
    /// <summary>
    /// displays files that will be copied to a folder
    /// </summary>
    public sealed class StagingsViewModel : ViewModelListBase<StagingViewModel>
    {
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly ILogger _log;
        private readonly LogViewModel _logViewModel;
        private readonly BackupsViewModel _backupsViewModel;

        private bool _reuseLastBackup;
        public bool ReuseLastBackup
        {
            get { return _reuseLastBackup; }
            set { SetValue(ref _reuseLastBackup, value); }
        }

        public ICommand ApplyCommand { get; }

        public Action OnApplyingStagings { get; set; }

        public StagingsViewModel(in IScarletCommandBuilder commandBuilder, ConfigurationViewModel configurationViewModel, ILogger log, LogViewModel logViewModel, BackupsViewModel backupsViewModel)
            : base(commandBuilder)
        {
            _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _log = log?.ForContext<StagingsViewModel>() ?? throw new ArgumentNullException(nameof(log));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _backupsViewModel = backupsViewModel ?? throw new ArgumentNullException(nameof(backupsViewModel));

            ApplyCommand = commandBuilder.Create(Apply, CanApply)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();
        }

        public async Task Add(string[] fileSystemInfos)
        {
            foreach (var fileSystemInfo in fileSystemInfos)
            {
                if (File.Exists(fileSystemInfo))
                {
                    var viewModel = new StagingViewModel(fileSystemInfo);

                    await Add(viewModel);
                }

                if (Directory.Exists(fileSystemInfo))
                {
                    var files = await Task.Run(() => Directory.GetFiles(fileSystemInfo, "*", SearchOption.AllDirectories));

                    foreach (var file in files)
                    {
                        var viewModel = new StagingViewModel(file);

                        await Add(viewModel);
                    }
                }
            }
        }

        public async Task Apply(CancellationToken token)
        {
            try
            {
                var reuseLastBackup = ReuseLastBackup;
                var deploymentFolder = _configurationViewModel.DeploymentFolder;
                var backupFolder = _configurationViewModel.BackupFolder;
                var backupTypes = _configurationViewModel.BackupFileTypes.Items.Select(p => p.Extension).ToArray();
                var backupFileFolder = GetFolderName(_backupsViewModel.Items, backupFolder, reuseLastBackup);

                Directory.CreateDirectory(deploymentFolder);
                Directory.CreateDirectory(backupFolder);
                Directory.CreateDirectory(backupFileFolder);

                await _logViewModel.Clear(token).ConfigureAwait(false);

                var t1 = Dispatcher.Invoke(() => OnApplyingStagings?.Invoke());

                var t2 = Task.Run(() =>
                {
                    try
                    {
                        foreach (var newfile in Items)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            var fileName = Path.GetFileName(newfile.Path);
                            var deploymentFileName = Path.Combine(deploymentFolder, fileName);
                            var backupFileName = Path.Combine(backupFileFolder, fileName);

                            if (backupTypes.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
                            {
                                BackupFile(newfile.Path, backupFileName, reuseLastBackup);
                            }

                            Update(newfile.Path, deploymentFileName);
                        }

                        _log.Write(Serilog.Events.LogEventLevel.Information, "Applied staged files to {directory}", deploymentFolder);
                    }
                    catch (Exception ex)
                    {
                        _log.Write(Serilog.Events.LogEventLevel.Fatal, ex.ToString());
                    }
                }, token);

                await Task.WhenAll(t1, t2).ConfigureAwait(false);

                await Clear(token).ConfigureAwait(false);

                await _backupsViewModel.Refresh(token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }
        }

        private string GetFolderName(IEnumerable<BackupViewModel> backups, string rootFolder, bool reuseLastBackup)
        {
            if (reuseLastBackup)
            {
                var reuseBackup = backups.OrderByDescending(p => p.TimeStamp).First();
                return Path.Combine(rootFolder, reuseBackup.TimeStamp.FormatAsBackup());
            }
            else
            {
                return Path.Combine(rootFolder, DateTime.Now.FormatAsBackup());
            }
        }

        private bool CanApply()
        {
            return !IsBusy
                && _configurationViewModel.Validation.IsValid;
        }

        private void Update(string from, string to)
        {
            var extension = Path.GetExtension(from).ToLower();
            if (extension == ".zip")
            {
                CopyArchive(from, Path.GetDirectoryName(to), true);
            }
            else
            {
                if (Copy(from, to, true))
                {
                    _log.Write(Serilog.Events.LogEventLevel.Information, "Updated {targetFile}", to);
                }
            }
        }

        private void BackupFile(string from, string to, bool overwrite)
        {
            if (Copy(from, to, overwrite))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Created backup of {targetFile} @ {copy}", from, to);
            }
        }

        private void CopyArchive(string from, string to, bool overwrite)
        {
            if (FileUtils.ExtractFor<StagingsViewModel>(from, to, _log, overwrite))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Extracted {backup} to {copy}", from, to);
            }
        }

        private bool Copy(string from, string to, bool overwrite = false)
        {
            return FileUtils.CopyFor<StagingsViewModel>(from, to, _log, overwrite);
        }
    }
}
