using JetBrains.Annotations;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Dawn.Core.Features.Backups
{
    /// <summary>
    /// a file that belongs to an update
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed partial class BackupViewModel : ViewModelListBase<FileSystemViewModel>
    {
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly BackupsViewModel _backupsViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly ILogger _log;
        private readonly IFileSystem _fileSystem;

        private readonly Func<Task<bool>> _onDeleteRequested;
        private readonly Action _onDeleting;
        private readonly Func<BackupViewModel, BackupViewModel> _onMetaDataEdit;
        private readonly Action<BackupViewModel> _onDetectChanges;

        private readonly string _fullPath;
        public string FullPath
        {
            get { return _fullPath; }
            private init { SetProperty(ref _fullPath, value); }
        }

        private readonly string _name;
        public string Name
        {
            get { return _name; }
            private init
            {
                if (SetProperty(ref _name, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayName))]
        private string? _customName;

        private string? _comment;
        public string? Comment
        {
            get { return _comment; }
            set { SetProperty(ref _comment, value); }
        }

        private readonly DateTime _timeStamp;
        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            private init { SetProperty(ref _timeStamp, value); }
        }

        public string DisplayName =>
            string.IsNullOrEmpty(CustomName)
                ? $"Backup: {Name}"
                : $"{CustomName} - {Name}";

        public ICommand DeleteCommand { get; }
        public ICommand OpenExternallyCommand { get; }
        public ICommand LoadMetaDataCommand { get; }
        public ICommand EditMetaDataCommand { [UsedImplicitly] get; }
        public ICommand DetectChangesCommand { get; }

        public BackupViewModel(
            IScarletCommandBuilder commandBuilder,
            IFileSystem fileSystem,
            BackupModel model,
            BackupsViewModel backupsViewModel,
            LogViewModel logViewModel,
            ILogger log,
            ConfigurationViewModel configurationViewModel,
            Func<Task<bool>> onDeleteRequested,
            Action onDeleting,
            Func<BackupViewModel, BackupViewModel> onMetaDataEdit,
            Action<BackupViewModel> onDetectChanges)
            : base(commandBuilder)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _backupsViewModel = backupsViewModel ?? throw new ArgumentNullException(nameof(backupsViewModel));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _log = log.ForContext<BackupViewModel>() ?? throw new ArgumentNullException(nameof(log));
            _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _onDeleteRequested = onDeleteRequested ?? throw new ArgumentNullException(nameof(onDeleteRequested));
            _onDeleting = onDeleting ?? throw new ArgumentNullException(nameof(onDeleting));
            _onMetaDataEdit = onMetaDataEdit ?? throw new ArgumentNullException(nameof(onMetaDataEdit));
            _onDetectChanges = onDetectChanges ?? throw new ArgumentNullException(nameof(onDetectChanges));

            _fullPath = model.FullPath ?? throw new ArgumentNullException(nameof(BackupModel.FullPath));
            _name = model.Name ?? throw new ArgumentNullException(nameof(BackupModel.Name));
            _timeStamp = model.TimeStamp;

            DeleteCommand = commandBuilder
                .Create(DeleteImpl, CanDeleteImpl)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();

            OpenExternallyCommand = commandBuilder
                .Create(OpenExternallyImpl)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();

            LoadMetaDataCommand = commandBuilder
                .Create(LoadMetaDataImpl, CanLoadMetaDataImpl)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();

            EditMetaDataCommand = commandBuilder
                .Create(EditMetaDataImpl, CanEditMetaDataImpl)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();

            DetectChangesCommand = commandBuilder
                .Create(DetectChangesImpl, CanDetectChangesImpl)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .Build();
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        private BackupViewModel(in IScarletCommandBuilder commandBuilder, BackupViewModel backupViewModel)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
            : base(commandBuilder)
        {
            FullPath = backupViewModel.FullPath ?? throw new ArgumentNullException(nameof(BackupModel.FullPath));
            Name = backupViewModel.Name ?? throw new ArgumentNullException(nameof(BackupModel.Name));
            TimeStamp = backupViewModel.TimeStamp;

            CustomName = backupViewModel.CustomName;
            Comment = backupViewModel.Comment;
            _fileSystem = backupViewModel._fileSystem;
        }

        private string GetMetaDataFileName()
        {
            return Path.Combine(FullPath, IFileSystem.MetaDataFileName);
        }

        private Task DetectChangesImpl()
        {
            _onDetectChanges.Invoke(this);

            return Task.CompletedTask;
        }

        private bool CanDetectChangesImpl()
        {
            return !IsBusy;
        }

        private Task LoadMetaDataImpl()
        {
            return Task.Run(() =>
            {
                var fileName = GetMetaDataFileName();
                if (!_fileSystem.FileExists(fileName))
                {
                    return;
                }

                var json = _fileSystem.ReadAllText(fileName, Encoding.UTF8);
                var model = JsonSerializer.Deserialize<BackupMetaDataModel>(json);

                if (model is null)
                {
                    return;
                }

                if (model.Comment?.Length > 0)
                {
                    Comment = model.Comment;
                }

                if (model.Name?.Length > 0)
                {
                    CustomName = model.Name;
                }
            });
        }

        private bool CanLoadMetaDataImpl()
        {
            return _fileSystem.FileExists(GetMetaDataFileName());
        }

        private Task EditMetaDataImpl()
        {
            return Task.Run(() =>
            {
                var copy = new BackupViewModel(CommandBuilder, this);
                copy = _onMetaDataEdit.Invoke(copy);

                Comment = copy.Comment?.Length > 0
                    ? copy.Comment
                    : null;

                CustomName = copy.CustomName?.Length > 0
                    ? copy.CustomName
                    : null;

                var fileName = GetMetaDataFileName();
                if (CustomName is null && Comment is null && _fileSystem.FileExists(fileName))
                {
                    _fileSystem.DeleteFile(fileName);
                    return;
                }

                var json = JsonSerializer.Serialize(new BackupMetaDataModel()
                {
                    Name = CustomName,
                    Comment = Comment
                });

                _fileSystem.WriteAllText(fileName, json, Encoding.UTF8);
            });
        }

        private bool CanEditMetaDataImpl()
        {
            return true;
        }

        private Task OpenExternallyImpl()
        {
            return Task.Run(() =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var info = new ProcessStartInfo("cmd", $"/c start {FullPath}")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using var process = Process.Start(info)!;
                    process.WaitForExit();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    using var process = Process.Start("xdg-open", FullPath);
                    process.WaitForExit();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    using var process = Process.Start("open", FullPath);
                    process.WaitForExit();
                }
            });
        }

        private async Task DeleteImpl()
        {
            var shouldDelete = await _onDeleteRequested.Invoke();
            if (!shouldDelete)
            {
                return;
            }

            _logViewModel.PrepareBegin();

            await Dispatcher.Invoke(() => _onDeleting.Invoke());

            _logViewModel.Begin();

            await Delete();

            _logViewModel.Complete();
        }

        public async Task Delete()
        {
            try
            {
                _log.Write(Serilog.Events.LogEventLevel.Warning, "Deleting backup {BackupName} in {FolderPath}", Name, FullPath);

                await Task.Run(() => _fileSystem.DeleteDirectory(_fullPath, true)).ConfigureAwait(false);
                await _backupsViewModel.Remove(this).ConfigureAwait(false);

                _log.Write(Serilog.Events.LogEventLevel.Information, "Deleted backup {BackupName}", Name);
            }
            catch (Exception ex)
            {
                _log.LogError(ex);
            }
        }

        private bool CanDeleteImpl()
        {
            return _configurationViewModel.HasErrors == false
                && _fullPath.Length > 0
                && _fileSystem.DirectoryExists(_fullPath);
        }

        private string GetDebuggerDisplay()
        {
            return CustomName ?? Name;
        }
    }
}
