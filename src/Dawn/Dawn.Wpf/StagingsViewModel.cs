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

        public ICommand ApplyCommand { get; }

        public Action OnApplyingStagings { get; set; }

        public StagingsViewModel(in IScarletCommandBuilder commandBuilder, ConfigurationViewModel configurationViewModel, ILogger log, LogViewModel logViewModel)
            : base(commandBuilder)
        {
            _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
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
            var targetPath = _configurationViewModel.BackupFolder;
            var backupTypes = _configurationViewModel.BackupFileTypes.Items.Select(p => p.Extension).ToArray();
            var pattern = _configurationViewModel.FilePattern;

            await _logViewModel.Clear(token).ConfigureAwait(false);

            _log.Write(Serilog.Events.LogEventLevel.Debug, "Debug");
            _log.Write(Serilog.Events.LogEventLevel.Information, "Information");
            _log.Write(Serilog.Events.LogEventLevel.Warning, "Warning");
            _log.Write(Serilog.Events.LogEventLevel.Error, "Error");
            _log.Write(Serilog.Events.LogEventLevel.Fatal, "Fatal");

            var t1 = Dispatcher.Invoke(() => OnApplyingStagings?.Invoke());

            var t2 = Task.Run(() =>
            {
                try
                {
                    foreach (var file in Items)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        try
                        {
                            var fileName = Path.GetFileName(file.Path);
                            var extension = Path.GetExtension(fileName).ToLowerInvariant(); ;
                            var targetFile = Path.Combine(targetPath, fileName);

                            if (File.Exists(targetFile))
                            {
                                if (backupTypes.Contains(extension))
                                {
                                    var copy = $"{Path.Combine(targetPath, fileName.Replace(extension, ""))}{pattern}{extension}";
                                    if (File.Exists(copy))
                                    {
                                        _log.Write(Serilog.Events.LogEventLevel.Warning, "Backup exists already for {targetFile} @  {copy}", targetFile, copy);
                                    }
                                    else
                                    {
                                        File.Move(targetFile, copy);
                                        _log.Write(Serilog.Events.LogEventLevel.Debug, "Created backup of {targetFile} @ {copy}", targetFile, copy);
                                    }
                                }

                                File.Copy(file.Path, targetFile, true);
                                _log.Write(Serilog.Events.LogEventLevel.Information, "Updated {targetFile}", targetFile);
                            }
                            else
                            {
                                File.Copy(file.Path, targetFile);
                                _log.Write(Serilog.Events.LogEventLevel.Information, "Added {targetFile}", targetFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Write(Serilog.Events.LogEventLevel.Fatal, ex.ToString());
                }
            }, token);

            await Task.WhenAll(t1, t2).ConfigureAwait(false);

            await Clear(token).ConfigureAwait(false);
        }

        private bool CanApply()
        {
            return !IsBusy;
        }
    }
}
