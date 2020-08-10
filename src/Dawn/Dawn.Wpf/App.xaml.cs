using DryIoc;
using Jot;
using Serilog;
using System.Windows;

namespace Dawn.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IContainer _container;
        private readonly Tracker _tracker;
        private readonly ConfigurationService _configurationService;

        public App()
        {
            _container = CompositionRoot.Get();
            _tracker = _container.Resolve<Tracker>();

            _configurationService = _container.Resolve<ConfigurationService>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var shell = _container.Resolve<Shell>();
            shell.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tracker.PersistAll();
            _configurationService.Save();

            Log.CloseAndFlush();

            _container.Dispose();

            base.OnExit(e);
        }
    }
}
