using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
{
    /// <summary>
    /// a file that belongs to an update
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class BackupViewModel : ViewModelListBase<FileSystemViewModel>
    {
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly BackupsViewModel _backupsViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly ILogger _log;
        private readonly IFileSystem _fileSystem;

        private readonly Func<bool> _onDeleteRequested;
        private readonly Action _onDeleting;
        private readonly Func<BackupViewModel, BackupViewModel> _onMetaDataEdit;
        private readonly Action<BackupViewModel> _onDetectChanges;

        private string _fullPath;
        public string FullPath
        {
            get { return _fullPath; }
            private set { SetProperty(ref _fullPath, value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            private set { SetProperty(ref _name, value); }
        }

        private string _customName;
        public string CustomName
        {
            get { return _customName; }
            set { SetProperty(ref _customName, value); }
        }

        private string _comment;
        public string Comment
        {
            get { return _comment; }
            set { SetProperty(ref _comment, value); }
        }

        private DateTime _timeStamp;
        public DateTime TimeStamp
        {
            get { return _timeStamp; }
            private set { SetProperty(ref _timeStamp, value); }
        }

        public ICommand DeleteCommand { get; }
        public ICommand OpenExternallyCommand { get; }
        public ICommand LoadMetaDataCommand { get; }
        public ICommand EditMetaDataCommand { get; }
        public ICommand DetectChangesCommand { get; }

        public BackupViewModel(in IScarletCommandBuilder commandBuilder,
                               IFileSystem fileSystem,
                               BackupModel model,
                               BackupsViewModel backupsViewModel,
                               LogViewModel logViewModel,
                               ILogger log,
                               ConfigurationViewModel configurationViewModel,
                               Func<bool> onDeleteRequested,
                               Action onDeleting,
                               Func<BackupViewModel, BackupViewModel> onMetaDataEdit,
                               Action<BackupViewModel> onDetectChanges)
            : base(commandBuilder)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _backupsViewModel = backupsViewModel ?? throw new ArgumentNullException(nameof(backupsViewModel));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _log = log?.ForContext<BackupViewModel>() ?? throw new ArgumentNullException(nameof(log));
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

        private BackupViewModel(in IScarletCommandBuilder commandBuilder, BackupViewModel backupViewModel)
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
                var model = JsonConvert.DeserializeObject<BackupMetaDataModel>(json);

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
                var name = _onMetaDataEdit.Invoke(copy);

                if (copy.Comment?.Length > 0)
                {
                    Comment = copy.Comment;
                }
                else
                {
                    Comment = null;
                }

                if (copy.CustomName?.Length > 0)
                {
                    CustomName = copy.CustomName;
                }
                else
                {
                    CustomName = null;
                }

                var fileName = GetMetaDataFileName();
                if (CustomName is null && Comment is null && _fileSystem.FileExists(fileName))
                {
                    _fileSystem.DeleteFile(fileName);
                    return;
                }

                var json = JsonConvert.SerializeObject(new BackupMetaDataModel()
                {
                    Name = CustomName,
                    Comment = Comment
                });

                _fileSystem.WriteAllText(fileName, json, Encoding.UTF8);
            });
        }

        private bool CanEditMetaDataImpl()
        {
            return _onMetaDataEdit != null;
        }

        private Task OpenExternallyImpl()
        {
            return Task.Run(() =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (var process = Process.Start(new ProcessStartInfo("cmd", $"/c start {FullPath}")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }))
                    {
                        process.WaitForExit();
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    using (var process = Process.Start("xdg-open", FullPath))
                    {
                        process.WaitForExit();
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    using (var process = Process.Start("open", FullPath))
                    {
                        process.WaitForExit();
                    }
                }
            });
        }

        private async Task DeleteImpl()
        {
            var shouldDelete = _onDeleteRequested?.Invoke() ?? false;

            if (!shouldDelete)
            {
                return;
            }

            _logViewModel.PrepareBegin();

            var t1 = Dispatcher.Invoke(() => _onDeleting?.Invoke());
            var t2 = Task.Run(async () =>
            {
                var subscription = default(IDisposable);
                try
                {
                    if (!_backupsViewModel.IsBusy)
                    {
                        // mass operation in progress
                        subscription = _logViewModel.Begin();
                    }

                    _log.Write(Serilog.Events.LogEventLevel.Warning, "Deleting backup {BackupName} in {FolderPath}", Name, FullPath);

                    await Task.Run(() => _fileSystem.DeleteDirectory(_fullPath, true)).ConfigureAwait(false);
                    await _backupsViewModel.Remove(this).ConfigureAwait(false);

                    _log.Write(Serilog.Events.LogEventLevel.Information, "Deleted backup {BackupName}", Name);
                }
                finally
                {
                    subscription?.Dispose();
                }
            });

            await Task.WhenAll(t1, t2).ConfigureAwait(false);
        }

        public async Task Delete()
        {
            try
            {
                await DeleteImpl().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex);
            }
        }

        private bool CanDeleteImpl()
        {
            return _configurationViewModel?.HasErrors == false
                && _onDeleteRequested != null
                && _onDeleting != null
                && _log != null
                && _logViewModel != null
                && _fullPath.Length > 0
                && _fileSystem.DirectoryExists(_fullPath);
        }

        private string GetDebuggerDisplay()
        {
            return CustomName ?? Name;
        }
    }
}
