using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Octokit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public sealed class ShellViewModel : ViewModelBase
    {
        private const string ZipContentType = "application/x-zip-compressed";
        private const string ZipDownloadType = "application/octet-stream";
        private const string GithubRepositoryOwner = "insire";

        private readonly GitHubClient _client;
        private readonly AboutViewModel _aboutViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly ILogger _log;

        private bool _hasUpdatedApplication;
        private ReleaseAsset _asset;

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

        public Func<bool> OnApplicationUpdated { get; set; }

        public ICommand CheckForApplicationUpdateCommand { get; }

        public ICommand GetApplicationUpdateCommand { get; }
        public ICommand ShowLogCommand { get; }

        public Action ShowLogAction { get; set; }

        public ShellViewModel(ConfigurationViewModel configuration, BackupsViewModel updates, StagingsViewModel stagings, AboutViewModel aboutViewModel, LogViewModel logViewModel, ILogger log)
            : base(ScarletCommandBuilder.Default)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Updates = updates ?? throw new ArgumentNullException(nameof(updates));
            Stagings = stagings ?? throw new ArgumentNullException(nameof(stagings));

            _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _log = log?.ForContext<ShellViewModel>() ?? throw new ArgumentNullException(nameof(log));

            _client = new GitHubClient(new ProductHeaderValue(GithubRepositoryOwner));

            Title = $"{aboutViewModel.Product} v{aboutViewModel.AssemblyVersionString}";

            CheckForApplicationUpdateCommand = ScarletCommandBuilder.Default
                .Create(CheckForApplicationUpdate, CanCheckForApplicationUpdate)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .WithCancellation()
                .Build();

            GetApplicationUpdateCommand = ScarletCommandBuilder.Default
                .Create(GetApplicationUpdate, CanGetApplicationUpdate)
                .WithBusyNotification(BusyStack)
                .WithSingleExecution()
                .WithCancellation()
                .Build();

            ShowLogCommand = ScarletCommandBuilder.Default
                .Create(ShowLog)
                .WithSingleExecution()
                .WithCancellation()
                .Build();
        }

        private Task ShowLog()
        {
            ShowLogAction?.Invoke();

            return Task.CompletedTask;
        }

        private async Task CheckForApplicationUpdate(CancellationToken token)
        {
            _log.Write(Serilog.Events.LogEventLevel.Debug, "Checking for updates");

            var releases = await _client.Repository.Release.GetAll(GithubRepositoryOwner, "dawn");
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

            if (Version.TryParse(latest.TagName, out var version))
            {
                if (_aboutViewModel.AssemblyVersion < version)
                {
                    IsApplicationUpdateAvailable = true;
                    HasCheckedForApplicationUpdate = true;

                    _log.Write(Serilog.Events.LogEventLevel.Information, "An update ({release}) is available", latest.TagName);

                    _asset = latest.Assets.FirstOrDefault(p => p.ContentType == ZipContentType);

                    return;
                }
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
            try
            {
                _logViewModel.Progress.Report(0);
                var tempDirectory = Path.GetTempPath();
                var tempZipFile = Path.Combine(tempDirectory, $"{_asset.Name}");
                var tempExtractDirectory = Path.Combine(tempDirectory, Path.GetFileNameWithoutExtension(_asset.Name));

                _log.Write(Serilog.Events.LogEventLevel.Debug, "Downloading release from {url}", _asset.Url);
                if (!await DownloadRelease(tempZipFile, token))
                {
                    return;
                }

                _log.Write(Serilog.Events.LogEventLevel.Debug, "Extracting release to {directory}", tempExtractDirectory);
                if (!FileUtils.ExtractFor<ShellViewModel>(tempZipFile, tempExtractDirectory, _log, _logViewModel.Progress, true))
                {
                    return;
                }

                var thisProcess = ReplaceApplicaionFiles(tempExtractDirectory);

                _log.Write(Serilog.Events.LogEventLevel.Information, "Update to {version} completed succssfully", _asset.Name);

                _log.Write(Serilog.Events.LogEventLevel.Debug, "Cleaning up temporary files");
                CleanUpFiles(tempDirectory);

                _logViewModel.Progress.Report(100);
                _hasUpdatedApplication = true;
                if (OnApplicationUpdated?.Invoke() == true)
                {
                    _log.Write(Serilog.Events.LogEventLevel.Information, "Restarting application.");
                    Restart(thisProcess);
                }
            }
            catch (Exception ex)
            {
                _log.Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }
        }

        private void Restart(Process thisProcess)
        {
            _log.Write(Serilog.Events.LogEventLevel.Debug, "Spawning new process.");

            var spawn = Process.Start(thisProcess.MainModule.FileName);

            _log.Write(Serilog.Events.LogEventLevel.Debug, "New process ID is {0}", spawn.Id);
            _log.Write(Serilog.Events.LogEventLevel.Debug, "Closing old running process {0}.", thisProcess.Id);

            thisProcess.CloseMainWindow();
            thisProcess.Close();
            thisProcess.Dispose();
        }

        private Process ReplaceApplicaionFiles(string from)
        {
            var thisProcess = Process.GetCurrentProcess();

            var me = thisProcess.MainModule.FileName;
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
            var files = Directory.GetFiles(from, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.EndsWith(".pdb") && Debugger.IsAttached)
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
            var fileSystemInfos = Directory.GetFiles(directory, "Dawn*.zip", SearchOption.TopDirectoryOnly);
            foreach (var file in fileSystemInfos)
            {
                DeleteFile(file);
            }

            foreach (var subDirectory in Directory.GetDirectories(directory, "Dawn*", SearchOption.TopDirectoryOnly))
            {
                fileSystemInfos = Directory.GetFiles(subDirectory, "*", SearchOption.AllDirectories);
                foreach (var file in fileSystemInfos)
                {
                    DeleteFile(file);
                }
            }
        }

        private async Task<bool> DownloadRelease(string to, CancellationToken token)
        {
            var response = await _client.Connection.Get<object>(new Uri(_asset.Url), new Dictionary<string, string>(), ZipDownloadType, token);

            if (response?.HttpResponse?.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            var responseData = response.HttpResponse.Body;

            _log.Write(Serilog.Events.LogEventLevel.Debug, "Writing release to {file}", to);
            File.WriteAllBytes(to, (byte[])responseData);

            return true;
        }

        private bool CanGetApplicationUpdate()
        {
            return !_hasUpdatedApplication;
        }

        private void MoveFile(string from, string to)
        {
            if (FileUtils.MoveFor<ShellViewModel>(from, to, _log, true))
            {
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Moving {from} from {to}", from, to);
            }
        }

        private void DeleteFile(string from)
        {
            if (FileUtils.DeleteFor<ShellViewModel>(from, _log))
            {
                _log.Write(Serilog.Events.LogEventLevel.Warning, "Deleting {from}", from);
            }
        }
    }
}
