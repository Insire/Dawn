using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using System;
using System.IO;
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
        private readonly Func<bool> _onDeleteRequested;

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

        public BackupViewModel(in IScarletCommandBuilder commandBuilder, string fullPath, string name, DateTime timeStamp, BackupsViewModel backupsViewModel, Func<bool> onDeleteRequested)
            : base(commandBuilder)
        {
            _backupsViewModel = backupsViewModel ?? throw new ArgumentNullException(nameof(backupsViewModel));
            _onDeleteRequested = onDeleteRequested ?? throw new ArgumentNullException(nameof(onDeleteRequested));

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
            var shouldDelete = _onDeleteRequested?.Invoke() ?? false;

            if (!shouldDelete)
            {
                return;
            }

            await Task.Run(() => Directory.Delete(_fullPath, true));
            await _backupsViewModel.Remove(this);
        }

        private bool CanDelete()
        {
            return _fullPath.Length > 0 && Directory.Exists(_fullPath);
        }
    }
}
