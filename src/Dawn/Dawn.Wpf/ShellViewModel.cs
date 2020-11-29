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
using System.Reflection;
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

        private readonly Version _applicationVersion;
        private readonly GitHubClient _client;
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
            private set { SetValue(ref _isApplicationUpdateAvailable, value); }
        }

        public ICommand CheckForApplicationUpdateCommand { get; }

        public ICommand GetApplicationUpdateCommand { get; }

        public Func<bool> OnApplicationUpdated { get; set; }

        private bool _hasCheckedForApplicationUpdate;
        public bool HasCheckedForApplicationUpdate
        {
            get { return _hasCheckedForApplicationUpdate; }
            private set { SetValue(ref _hasCheckedForApplicationUpdate, value); }
        }

        public ShellViewModel(ConfigurationViewModel configuration, BackupsViewModel updates, StagingsViewModel stagings, ILogger log, Assembly assembly)
            : base(ScarletCommandBuilder.Default)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Updates = updates ?? throw new ArgumentNullException(nameof(updates));
            Stagings = stagings ?? throw new ArgumentNullException(nameof(stagings));

            _log = log?.ForContext<ShellViewModel>() ?? throw new ArgumentNullException(nameof(log));
            _applicationVersion = GetVersion(assembly);
            _client = new GitHubClient(new ProductHeaderValue(GithubRepositoryOwner));

            Title = $"Dawn v{_applicationVersion.ToString(3)}";

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
        }

        private static Version GetVersion(Assembly assembly)
        {
            var attributes = assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
            if (attributes.Length == 0)
            {
                return assembly.GetName()?.Version ?? new Version(0, 0, 0, 0);
            }

            var version = attributes.Cast<AssemblyMetadataAttribute>().FirstOrDefault(p => p.Key == "Version");

            return new Version(version.Value);
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

                _log.Write(Serilog.Events.LogEventLevel.Debug, "No releases available");
                return;
            }

            if (Version.TryParse(latest.Name, out var version))
            {
                if (_applicationVersion < version)
                {
                    IsApplicationUpdateAvailable = true;
                    HasCheckedForApplicationUpdate = true;

                    _log.Write(Serilog.Events.LogEventLevel.Debug, "An update ({release}) is available", latest.Name);

                    _asset = latest.Assets.FirstOrDefault(p => p.ContentType == ZipContentType);

                    return;
                }
            }

            IsApplicationUpdateAvailable = false;
            HasCheckedForApplicationUpdate = true;
            _log.Write(Serilog.Events.LogEventLevel.Debug, "No update available");
        }

        private bool CanCheckForApplicationUpdate()
        {
            return !HasCheckedForApplicationUpdate;
        }

        private async Task GetApplicationUpdate(CancellationToken token)
        {
            try
            {
                var tempDirectory = Path.GetTempPath();
                var tempZipFile = Path.Combine(tempDirectory, $"{_asset.Name}");
                var tempExtractDirectory = Path.Combine(tempDirectory, Path.GetFileNameWithoutExtension(_asset.Name));

                _log.Write(Serilog.Events.LogEventLevel.Debug, "Downloading release from {url}", _asset.Url);
                if (!await DownloadRelease(tempZipFile, token))
                {
                    return;
                }

                _log.Write(Serilog.Events.LogEventLevel.Debug, "Extracting release to {directory}", tempExtractDirectory);
                if (!FileUtils.ExtractFor<ShellViewModel>(tempZipFile, tempExtractDirectory, _log, true))
                {
                    return;
                }

                var thisProcess = ReplaceApplicaionFiles(tempExtractDirectory);

                _log.Write(Serilog.Events.LogEventLevel.Information, "Update to {version} completed succssfully", _asset.Name);

                _log.Write(Serilog.Events.LogEventLevel.Debug, "Cleaning up temporary files");
                CleanUpFiles(tempDirectory);

                _hasUpdatedApplication = true;
                if (OnApplicationUpdated?.Invoke() == true)
                {
                    _log.Write(Serilog.Events.LogEventLevel.Debug, "Restarting application.");
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
                _log.Write(Serilog.Events.LogEventLevel.Debug, "Deleting {from}", from);
            }
        }
    }
}
