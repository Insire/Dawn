using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Dawn.Core;
using Dawn.Core.Features.Configuration;
using DryIoc;
using Serilog;
using System.Linq;

namespace Dawn.Avalonia
{
    public partial class App : Application
    {
        private IClassicDesktopStyleApplicationLifetime? _applicationLifetime;
        private IContainer? _container;
        private ConfigurationService? _configurationService;

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
                var shell = container.Resolve<Shell>();
                shell.DataContext = container.Resolve<ShellViewModel>();

                desktop.MainWindow = shell;
            }

            base.OnFrameworkInitializationCompleted();
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
