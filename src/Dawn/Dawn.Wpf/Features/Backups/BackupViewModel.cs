using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
{
    /// <summary>
    /// a file that belongs to an update
    /// </summary>
    public sealed class BackupViewModel : ViewModelListBase<ViewModelContainer<string>>
    {
        private readonly BackupsViewModel _backupsViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly ILogger _log;
        private readonly Func<bool> _onDeleteRequested;
        private readonly Action _onDeleting;

        private string _fullPath;
        public string FullPath
        {
            get { return _fullPath; }
            private set { SetValue(ref _fullPath, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            private set { SetValue(ref _name, value); }
        }

        private DateTime _timeStamp;
        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            private set { SetValue(ref _timeStamp, value); }
        }

        public ICommand DeleteCommand { get; }

        public BackupViewModel(in IScarletCommandBuilder commandBuilder, string fullPath, string name, DateTime timeStamp, BackupsViewModel backupsViewModel, LogViewModel logViewModel, ILogger log, Func<bool> onDeleteRequested, Action onDeleting)
            : base(commandBuilder)
        {
            _backupsViewModel = backupsViewModel ?? throw new ArgumentNullException(nameof(backupsViewModel));
            _logViewModel = logViewModel;
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _onDeleteRequested = onDeleteRequested ?? throw new ArgumentNullException(nameof(onDeleteRequested));
            _onDeleting = onDeleting ?? throw new ArgumentNullException(nameof(onDeleting));

            FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TimeStamp = timeStamp;

            DeleteCommand = commandBuilder.Create(Delete, CanDelete)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();
        }

        public async Task Delete()
        {
            try
            {
                var shouldDelete = _onDeleteRequested?.Invoke() ?? false;

                if (!shouldDelete)
                {
                    return;
                }

                if (!_backupsViewModel.IsBusy)
                {
                    // mass operation in progress
                    await _logViewModel.Clear(CancellationToken.None).ConfigureAwait(false);
                }

                _log.ForContext<BackupViewModel>().Write(Serilog.Events.LogEventLevel.Warning, "Deleting backup {name} in {path}", Name, FullPath);

                var t1 = Dispatcher.Invoke(() => _onDeleting?.Invoke());
                var t2 = Task.Run(async () =>
                {
                    await Task.Run(() => Directory.Delete(_fullPath, true));
                    await _backupsViewModel.Remove(this);
                });

                await Task.WhenAll(t1, t2).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }
        }

        private bool CanDelete()
        {
            return _fullPath.Length > 0 && Directory.Exists(_fullPath);
        }
    }
}
