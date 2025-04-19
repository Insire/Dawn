using Dawn.Core.Features.About;
using Dawn.Core.Features.Backups;
using Dawn.Core.Features.Staging;
using DynamicData.Binding;
using JetBrains.Annotations;
using Octokit;
using System.Diagnostics;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Dawn.Core
{
    public sealed class ShellViewModel : ViewModelBase
    {
        private const string ZipCompressedContentType = "application/x-zip-compressed";
        private const string ZipContentType = "application/zip";

        private const string ZipDownloadType = "application/octet-stream";
        private const string GithubRepositoryOwner = "insire";

        private readonly GitHubClient _client;
        private readonly AboutViewModel _aboutViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _log;
        private readonly CompositeDisposable _disposables;

        private bool _hasUpdatedApplication;
        private ReleaseAsset? _asset;

        public string Title { get; }

        public ConfigurationViewModel Configuration { get; }

        public BackupsViewModel Updates { get; }

        public StagingsViewModel Stagings { get; }

        private bool _isApplicationUpdateAvailable;
        public bool IsApplicationUpdateAvailable
        {
            get { return _isApplicationUpdateAvailable; }
            private set { SetProperty(ref _isApplicationUpdateAvailable, value); }
        }

        private bool _hasCheckedForApplicationUpdate;
        public bool HasCheckedForApplicationUpdate
        {
            get { return _hasCheckedForApplicationUpdate; }
            private set { SetProperty(ref _hasCheckedForApplicationUpdate, value); }
        }

        public bool IsEmpty => Stagings.IsEmpty && !Updates.HasItems;

        public Func<Task<bool>>? OnApplicationUpdated { get; set; }

        public ICommand CheckForApplicationUpdateCommand { [UsedImplicitly] get; }

        public ICommand GetApplicationUpdateCommand { [UsedImplicitly] get; }
        public ICommand ShowLogCommand { [UsedImplicitly] get; }

        public Action? ShowLogAction { get; set; }

        public ShellViewModel(ConfigurationViewModel configuration,
                              BackupsViewModel updates,
                              StagingsViewModel stagings,
                              AboutViewModel aboutViewModel,
                              LogViewModel logViewModel,
                              ILogger log,
                              IFileSystem fileSystem,
                              IScarletCommandBuilder commandBuilder,
                              SynchronizationContext context)
            : base(commandBuilder)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Updates = updates ?? throw new ArgumentNullException(nameof(updates));
            Stagings = stagings ?? throw new ArgumentNullException(nameof(stagings));

            _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            _log = log.ForContext<ShellViewModel>() ?? throw new ArgumentNullException(nameof(log));

            _client = new GitHubClient(new ProductHeaderValue(GithubRepositoryOwner));

            Title = $"{aboutViewModel.Product} v{aboutViewModel.AssemblyVersionString}";

            CheckForApplicationUpdateCommand = commandBuilder
                .Create(CheckForApplicationUpdate, CanCheckForApplicationUpdate)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .WithCancellation()
                .Build();

            GetApplicationUpdateCommand = commandBuilder
                .Create(GetApplicationUpdate, CanGetApplicationUpdate)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .WithCancellation()
                .Build();

            ShowLogCommand = commandBuilder
                .Create(ShowLog)
                .WithSingleExecution()
                .WithCancellation()
                .Build();

            var subscription1 = Updates
                .WhenPropertyChanged(p => p.HasItems, notifyOnInitialValue: true)
                .ObserveOn(context)
                .Subscribe(_ =>
                {
                    OnPropertyChanged(nameof(IsEmpty));
                });

            var subscription2 = Stagings
                .WhenPropertyChanged(p => p.IsEmpty, notifyOnInitialValue: true)
                .ObserveOn(context)
                .Subscribe(_ =>
                {
                    OnPropertyChanged(nameof(IsEmpty));
                });

            _disposables = new CompositeDisposable(subscription1, subscription2);
        }

        private Task ShowLog()
        {
            ShowLogAction?.Invoke();

            return Task.CompletedTask;
        }

        private async Task CheckForApplicationUpdate(CancellationToken token)
        {
            _log.Write(Serilog.Events.LogEventLevel.Debug, "Checking for updates");

            var releases = await _client.Repository.Release.GetAll(GithubRepositoryOwner, "dawn").ConfigureAwait(false);
            var publicReleases = releases
                .Where(p => !p.Draft && !p.Prerelease)
                .OrderByDescending(p => p.PublishedAt)
                .ToList();
            var latest = publicReleases.FirstOrDefault();

            if (latest is null)
            {
                IsApplicationUpdateAvailable = false;
                HasCheckedForApplicationUpdate = true;

                _log.Write(Serilog.Events.LogEventLevel.Warning, "No releases available");
                return;
            }

            if (Version.TryParse(latest.TagName, out var version) && _aboutViewModel.AssemblyVersion < version)
            {
                IsApplicationUpdateAvailable = true;
                HasCheckedForApplicationUpdate = true;

                _log.Write(Serilog.Events.LogEventLevel.Information, "An update ({release}) is available", latest.TagName);

                _asset = latest.Assets.FirstOrDefault(p => p.ContentType is ZipContentType or ZipCompressedContentType);

                return;
            }

            IsApplicationUpdateAvailable = false;
            HasCheckedForApplicationUpdate = true;
            _log.Write(Serilog.Events.LogEventLevel.Warning, "No update available");
        }

        private bool CanCheckForApplicationUpdate()
        {
            return !HasCheckedForApplicationUpdate;
        }

        private async Task GetApplicationUpdate(CancellationToken token)
        {
            var asset = _asset;
            if (asset is null)
            {
                return;
            }

            try
            {
                _logViewModel.Progress.Report(0);
                var tempDirectory = Path.GetTempPath();
                var tempZipFile = Path.Combine(tempDirectory, $"{asset.Name}");
                var tempExtractDirectory = Path.Combine(tempDirectory, Path.GetFileNameWithoutExtension(asset.Name));

                _log.Write(Serilog.Events.LogEventLevel.Debug, "Downloading release from {url}", asset.Url);
                if (!await DownloadRelease(asset, tempZipFile, token).ConfigureAwait(false))
                {
                    return;
                }

                _log.Write(Serilog.Events.LogEventLevel.Debug, "Extracting release to {directory}", tempExtractDirectory);
                if (!_fileSystem.ExtractFor<ShellViewModel>(tempZipFile, tempExtractDirectory, _log, DateTime.MinValue, _logViewModel.Progress, true))
                {
                    return;
                }

                var thisProcess = ReplaceApplicationFiles(tempExtractDirectory);

                _log.Write(Serilog.Events.LogEventLevel.Information, "Update to {version} completed successfully", asset.Name);

                _log.Write(Serilog.Events.LogEventLevel.Debug, "Cleaning up temporary files");
                CleanUpFiles(tempDirectory);

                _logViewModel.Complete();
                _hasUpdatedApplication = true;

                var onApplicationUpdated =OnApplicationUpdated;
                if (onApplicationUpdated is not null && await onApplicationUpdated.Invoke())
                {
                    _log.Write(Serilog.Events.LogEventLevel.Information, "Restarting application.");
                    Restart(thisProcess);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex);
            }
        }

        private void Restart(Process thisProcess)
        {
            _log.Write(Serilog.Events.LogEventLevel.Debug, "Spawning new process.");

            var spawn = Process.Start(thisProcess.MainModule!.FileName);

            _log.Write(Serilog.Events.LogEventLevel.Debug, "New process ID is {0}", spawn.Id);
            _log.Write(Serilog.Events.LogEventLevel.Debug, "Closing old running process {0}.", thisProcess.Id);

            thisProcess.CloseMainWindow();
            thisProcess.Close();
            thisProcess.Dispose();
        }

        private Process ReplaceApplicationFiles(string from)
        {
            var thisProcess = Process.GetCurrentProcess();

            var me = thisProcess.MainModule!.FileName;
            var currentDirectory = Path.GetDirectoryName(me);
            var bak = me + ".bak";

            if (File.Exists(bak))
            {
                File.Delete(bak);
            }

            _log.Write(Serilog.Events.LogEventLevel.Debug, "Changing the currently running executable so it can be overwritten");
            File.Move(me, bak);
            File.Copy(bak, me);

            _log.Write(Serilog.Events.LogEventLevel.Debug, "Updating application files");
            foreach (var file in _fileSystem.GetFiles(from, "*.*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".pdb", StringComparison.InvariantCultureIgnoreCase) && Debugger.IsAttached)
                {
                    continue; // the debugger holds a filelock on pdb files
                }

                var destination = file.Replace(from, currentDirectory);
                MoveFile(file, destination);
            }

            return thisProcess;
        }

        private void CleanUpFiles(string directory)
        {
            var fileSystemInfos = _fileSystem.GetFiles(directory, "Dawn*.zip", SearchOption.TopDirectoryOnly);
            foreach (var file in fileSystemInfos)
            {
                DeleteFile(file);
            }

            foreach (var subDirectory in Directory.GetDirectories(directory, "Dawn*", SearchOption.TopDirectoryOnly))
            {
                fileSystemInfos = _fileSystem.GetFiles(subDirectory, "*", SearchOption.AllDirectories);
                foreach (var file in fileSystemInfos)
                {
                    DeleteFile(file);
                }
            }
        }

        private async Task<bool> DownloadRelease(ReleaseAsset asset, string to, CancellationToken token)
        {
            var response = await _client.Connection.Get<object>(new Uri(asset.Url), new Dictionary<string, string>(), ZipDownloadType, token).ConfigureAwait(false);

            if (response?.HttpResponse?.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            var responseData = response.HttpResponse.Body;

            _log.Write(Serilog.Events.LogEventLevel.Debug, "Writing release to {file}", to);
            await File.WriteAllBytesAsync(to, (byte[])responseData, token);

            return true;
        }

        private bool CanGetApplicationUpdate()
        {
            return !_hasUpdatedApplication && _asset != null;
        }

        private void MoveFile(string from, string to)
        {
            if (_fileSystem.MoveFor<ShellViewModel>(from, to, _log, true))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Moving {from} from {to}", from, to);
            }
        }

        private void DeleteFile(string from)
        {
            if (_fileSystem.DeleteFor<ShellViewModel>(from, _log))
            {
                _log.Write(Serilog.Events.LogEventLevel.Warning, "Deleting {from}", from);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _disposables.Dispose();
            base.Dispose(disposing);
        }
    }
}
