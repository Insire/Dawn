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
    /// displays past updates
    /// </summary>
    public sealed class BackupsViewModel : BusinessViewModelListBase<BackupViewModel>
    {
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly ILogger _log;
        private bool _isMassDeleting;

        public ICommand RestoreCommand { get; }

        public ICommand DeleteAllCommand { get; }

        public Func<bool> OnDeleteAllRequested { get; set; }
        public Func<bool> OnDeleteRequested { get; set; }

        public Action OnDeletingAll { get; set; }

        public Action OnDeleting { get; set; }

        public Action OnRestoring { get; set; }

        public BackupsViewModel(in IScarletCommandBuilder commandBuilder, ConfigurationViewModel configurationViewModel, ILogger log, LogViewModel logViewModel)
            : base(commandBuilder)
        {
            _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));

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
                if (!Directory.Exists(_configurationViewModel.BackupFolder))
                {
                    return;
                }

                var lookup = new Dictionary<string, BackupViewModel>();
                var directories = await Task.Run(() => Directory.GetDirectories(_configurationViewModel.BackupFolder, "*", SearchOption.TopDirectoryOnly)).ConfigureAwait(false);

                foreach (var directory in directories)
                {
                    var key = Path.GetFileName(directory);
                    if (key.Length >= 8)
                    {
                        try
                        {
                            if (!int.TryParse(key.Substring(0, 2), out var days))
                                days = 1;
                            if (!int.TryParse(key.Substring(2, 2), out var months))
                                months = 1;
                            if (!int.TryParse(key.Substring(4, 4), out var years))
                                years = 2001;

                            var hours = 0;
                            var minutes = 0;
                            var seconds = 0;

                            if (key.Length >= 10)
                                int.TryParse(key.Substring(8, 2), out hours);
                            if (key.Length >= 12)
                                int.TryParse(key.Substring(10, 2), out minutes);
                            if (key.Length >= 14)
                                int.TryParse(key.Substring(12, 2), out seconds);

                            days--;
                            months--;
                            years--;

                            var date = DateTime.MinValue.AddDays(days);
                            date = date.AddMonths(months);
                            date = date.AddYears(years);
                            date = date.AddHours(hours);
                            date = date.AddMinutes(minutes);
                            date = date.AddSeconds(seconds);

                            key = date.ToString("yyyy.MM.dd hh:mm:ss");
                            if (!lookup.ContainsKey(key))
                            {
                                var group = new BackupViewModel(CommandBuilder, directory, key, date, this, _logViewModel, _log, () => _isMassDeleting || (OnDeleteRequested?.Invoke() ?? false), () => { if (!_isMassDeleting) OnDeleting?.Invoke(); });
                                var files = await Task.Run(() => Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly)).ConfigureAwait(false);

                                await group.AddRange(files.Select(p => new ViewModelContainer<string>(p))).ConfigureAwait(false);

                                lookup.Add(key, group);
                            }
                            else
                            {
                                await lookup[key].Add(new ViewModelContainer<string>(directory)).ConfigureAwait(false);
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                await AddRange(lookup.Values).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }
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

                await _logViewModel.Clear(token).ConfigureAwait(false);

                _log.ForContext<BackupsViewModel>().Write(Serilog.Events.LogEventLevel.Warning, "Deleting all backups in {path}", _configurationViewModel.BackupFolder);

                var t1 = Dispatcher.Invoke(() => OnDeletingAll?.Invoke());

                var t2 = Task.Run(async () =>
                {
                    try
                    {
                        _isMassDeleting = true;

                        for (int i = Items.Count - 1; i >= 0; i--)
                        {
                            BackupViewModel item = Items[i];
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            await item.Delete();
                        }

                        await Refresh(token);
                    }
                    finally
                    {
                        _isMassDeleting = false;
                    }
                });

                await Task.WhenAll(t1, t2).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }
        }

        private bool CanDeleteAll()
        {
            return !IsBusy && Items.Count > 0;
        }

        private async Task Restore(BackupViewModel backupViewModel, CancellationToken token)
        {
            try
            {
                var deploymentFolder = _configurationViewModel.DeploymentFolder;
                var backupFolder = _configurationViewModel.BackupFolder;
                var now = DateTime.Now;

                await _logViewModel.Clear(token).ConfigureAwait(false);

                _log.ForContext<BackupsViewModel>().Write(Serilog.Events.LogEventLevel.Information, "Restoring backup {path}", backupViewModel.Name);

                var t1 = Dispatcher.Invoke(() => OnRestoring?.Invoke());

                var t2 = Task.Run(() =>
                {
                    foreach (var file in backupViewModel.Items.ToArray())
                    {
                        var fileName = Path.GetFileName(file.Value);
                        var restoreFileName = Path.Combine(deploymentFolder, fileName);

                        Restore(file.Value, restoreFileName);

                        File.SetLastWriteTime(restoreFileName, now);
                    }
                });

                await Task.WhenAll(t1, t2).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }
        }

        private void Restore(string from, string to)
        {
            if (Copy(from, to))
            {
                _log.ForContext<BackupsViewModel>().Write(Serilog.Events.LogEventLevel.Debug, "Restored backup of {backup} from {copy}", to, from);
            }
        }

        private bool CanRestore(BackupViewModel backupViewModel)
        {
            return !IsBusy
                && backupViewModel != null
                && _configurationViewModel.BackupFolder != null
                && _configurationViewModel.BackupFolder.Length > 0
                && _configurationViewModel.DeploymentFolder != null
                && _configurationViewModel.DeploymentFolder.Length > 0;
        }

        private bool Copy(string from, string to)
        {
            return FileUtils.CopyFor<BackupsViewModel>(from, to, _log, true);
        }
    }
}
