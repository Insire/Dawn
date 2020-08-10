using AdonisUI.Converters;
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
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _backupsViewModel = backupsViewModel ?? throw new ArgumentNullException(nameof(backupsViewModel));

            ApplyCommand = commandBuilder.Create(Apply, CanApply)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();
        }

        public async Task Add(string[] files)
        {
            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    continue;
                }

                var viewModel = new StagingViewModel(file);

                await Add(viewModel);
            }
        }

        public async Task Apply(CancellationToken token)
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

                        // backup existing files
                        if (backupTypes.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
                        {
                            Backup(newfile.Path, backupFileName, reuseLastBackup);
                        }

                        Update(newfile.Path, deploymentFileName);
                    }
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
                && _configurationViewModel.BackupFolder != null
                && _configurationViewModel.BackupFolder.Length > 0
                 && _configurationViewModel.DeploymentFolder != null
                && _configurationViewModel.DeploymentFolder.Length > 0;
        }

        private void Update(string from, string to)
        {
            if (Copy(from, to, true))
            {
                _log.Write(Serilog.Events.LogEventLevel.Information, "Updated {targetFile}", to);
            }
        }

        private void Backup(string from, string to, bool overwrite)
        {
            if (Copy(from, to, overwrite))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Created backup of {targetFile} @ {copy}", from, to);
            }
        }

        private bool Copy(string from, string to, bool overwrite = false)
        {
            return FileUtils.Copy(from, to, _log, overwrite);
        }
    }
}
