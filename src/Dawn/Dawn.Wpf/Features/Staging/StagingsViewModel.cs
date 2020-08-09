using AdonisUI.Converters;
using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Serilog;
using System;
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
            var now = DateTime.Now;
            var targetFolder = _configurationViewModel.TargetFolder;
            var targetPath = _configurationViewModel.BackupFolder;
            var backupTypes = _configurationViewModel.BackupFileTypes.Items.Select(p => p.Extension).ToArray();
            var pattern = now.FormatAsBackup();
            var backUpPattern = now.AddSeconds(1).FormatAsBackup();

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
                        var tobeUpdated = Path.Combine(targetFolder, fileName);

                        var extension = Path.GetExtension(fileName).ToLowerInvariant();

                        // backup existing files
                        if (backupTypes.Contains(extension))
                        {
                            var tobeUpdatedBackup = $"{Path.Combine(targetPath, fileName.Replace(extension, ""))}{pattern}{extension}";
                            if (File.Exists(tobeUpdatedBackup))
                            {
                                _log.Write(Serilog.Events.LogEventLevel.Warning, "Backup exists already for {targetFile} @  {copy}", tobeUpdated, tobeUpdatedBackup);
                            }
                            else
                            {
                                if (File.Exists(tobeUpdated))
                                {
                                    Backup(tobeUpdated, tobeUpdatedBackup);
                                }
                                else
                                {
                                    _log.Write(Serilog.Events.LogEventLevel.Debug, "{targetFile} is a new file. No backup for existing file required", tobeUpdated);
                                }
                            }

                            // backup new files

                            Backup(newfile.Path, $"{Path.Combine(targetPath, fileName.Replace(extension, ""))}{backUpPattern}{extension}");
                        }

                        Update(newfile.Path, tobeUpdated);
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

        private bool CanApply()
        {
            return !IsBusy;
        }

        private void Update(string from, string to)
        {
            if (Copy(from, to, true))
            {
                _log.Write(Serilog.Events.LogEventLevel.Information, "Updated {targetFile}", to);
            }
        }

        private void Backup(string from, string to)
        {
            if (Copy(from, to))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Created backup of {targetFile} @ {copy}", from, to);
            }
        }

        private bool Copy(string from, string to, bool overwrite = false)
        {
            try
            {
                File.Copy(from, to, overwrite);
                return true;
            }
            catch (Exception ex)
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }

            return false;
        }
    }
}
