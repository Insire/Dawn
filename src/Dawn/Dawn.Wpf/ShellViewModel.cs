using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using Octokit;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public sealed class ShellViewModel : ViewModelBase
    {
        private readonly Version _applicationVersion;

        private bool _hasCheckedForApplicationUpdate;

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

        public ShellViewModel(ConfigurationViewModel configuration, BackupsViewModel updates, StagingsViewModel stagings, Assembly assembly)
            : base(ScarletCommandBuilder.Default)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Updates = updates ?? throw new ArgumentNullException(nameof(updates));
            Stagings = stagings ?? throw new ArgumentNullException(nameof(stagings));

            _applicationVersion = GetVersion(assembly);

            Title = $"Dawn v{_applicationVersion.ToString(3)}";

            CheckForApplicationUpdateCommand = ScarletCommandBuilder.Default
                .Create(CheckForApplicationUpdate, CanCheckForApplicationUpdate)
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
            var client = new GitHubClient(new ProductHeaderValue("Insire"));

            var releases = await client.Repository.Release.GetAll("insire", "dawn");
            var publicReleases = releases
                //.Where(p => !p.Draft && !p.Prerelease)
                .OrderByDescending(p => p.PublishedAt)
                .ToList();
            var latest = publicReleases.FirstOrDefault();

            if (latest is null)
            {
                IsApplicationUpdateAvailable = false;
                _hasCheckedForApplicationUpdate = true;
                return;
            }

            if (Version.TryParse(latest.Name, out var version))
            {
                if (_applicationVersion < version)
                {
                    IsApplicationUpdateAvailable = true;
                    _hasCheckedForApplicationUpdate = true;
                    return;
                }
            }

            IsApplicationUpdateAvailable = false;
            _hasCheckedForApplicationUpdate = true;
        }

        private bool CanCheckForApplicationUpdate()
        {
            return !_hasCheckedForApplicationUpdate;
        }
    }
}
