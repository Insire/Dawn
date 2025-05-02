using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Dawn.Avalonia.Infrastructure;
using Dawn.Core;
using Dawn.Core.Features.Configuration;
using DryIoc;
using MvvmScarletToolkit;
using Serilog;
using System.Linq;

namespace Dawn.Avalonia
{
    public partial class App : Application
    {
        private IClassicDesktopStyleApplicationLifetime? _applicationLifetime;
        private IContainer? _container;
        private ConfigurationService? _configurationService;
        private JsonFileStore? _jsonFileStore;
        private IScarletDispatcher? _dispatcher;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                _applicationLifetime = desktop;
                desktop.ShutdownRequested += OnShutDownRequested;

                var container = _container = CompositionRoot.Get(desktop);

                _configurationService = container.Resolve<ConfigurationService>();
                _jsonFileStore = container.Resolve<JsonFileStore>();
                _dispatcher = container.Resolve<IScarletDispatcher>();

                var shell = container.Resolve<Shell>();
                shell.DataContext = container.Resolve<ShellViewModel>();
                shell.Closing += OnShellClosing;

                var data = _jsonFileStore.GetData<ShellWindowData>(nameof(Shell));
                if (data != default)
                {
                    shell.Height = data.Height;
                    shell.Width = data.Width;
                    shell.Position = new PixelPoint(data.X, data.Y);

                    _ = _dispatcher?.Invoke(() =>
                    {
                        shell.WindowState = (WindowState)data.State;
                    }, System.Threading.CancellationToken.None);
                }

                desktop.MainWindow = shell;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnShellClosing(object? sender, WindowClosingEventArgs e)
        {
            if (sender is Shell shell)
            {
                shell.Closing -= OnShellClosing;

                var data = new ShellWindowData()
                {
                    Height = shell.Height,
                    Width = shell.Width,
                    X = shell.Position.X,
                    Y = shell.Position.Y,
                    State = (int)shell.WindowState,
                };

                _jsonFileStore?.SetData(nameof(Shell), data);
            }
        }

        private static void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }

        private void OnShutDownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            var applicationLifetime = _applicationLifetime;
            if (applicationLifetime is not null)
            {
                applicationLifetime.ShutdownRequested -= OnShutDownRequested;
            }

            _configurationService?.Save();
            _container?.Dispose();

            Log.CloseAndFlush();
        }
    }
}
