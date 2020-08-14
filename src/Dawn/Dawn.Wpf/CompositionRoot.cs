using DryIoc;
using FluentValidation;
using Jot;
using Jot.Storage;
using MvvmScarletToolkit;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Windows;

namespace Dawn.Wpf
{
    internal static class CompositionRoot
    {
        public static IContainer Get()
        {
            var c = new Container();

            var logViewModel = new LogViewModel(ScarletCommandBuilder.Default);

            var logConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                .WriteTo.Logger(lc =>
                    lc.Filter.ByIncludingOnly((o) => Matching.FromSource<StagingsViewModel>().Invoke(o)
                    || Matching.FromSource<BackupsViewModel>().Invoke(o)
                    || Matching.FromSource<BackupViewModel>().Invoke(o))
                    .WriteTo.Sink(logViewModel, LogEventLevel.Verbose))
                .WriteTo.RollingFile("./logs/log-{Date}.txt", buffered: false);

            c.UseInstance<ILogger>(logConfiguration.CreateLogger());
            c.UseInstance(logViewModel);
            c.UseInstance(Assembly.GetAssembly(typeof(CompositionRoot)));
            c.UseInstance(new HttpClient());
            c.UseInstance(Process.GetCurrentProcess());

            c.Register<Shell>(Reuse.Singleton);
            var tracker = new Tracker(new JsonFileStore(Environment.SpecialFolder.CommonApplicationData));
            tracker.Configure<Shell>()
                .Id(w => $"[Width={SystemParameters.VirtualScreenWidth},Height{SystemParameters.VirtualScreenHeight}]")
                .Properties(w => new { w.Height, w.Width, w.Left, w.Top, w.WindowState })
                .PersistOn(nameof(Window.Closing))
                .StopTrackingOn(nameof(Window.Closing));
            c.UseInstance(tracker);

            c.Register<ConfigurationService>(Reuse.Singleton);
            c.Register(made: Made.Of(r => ServiceInfo.Of<ConfigurationService>(), f => f.Get()));
            c.Register<IValidator<ConfigurationViewModel>, ConfigurationViewModelValidator>(Reuse.Singleton);

            c.Register<ShellViewModel>(Reuse.Singleton);
            c.Register<ConfigurationViewModel>(Reuse.Singleton);
            c.Register<StagingsViewModel>(Reuse.Singleton);
            c.Register<BackupsViewModel>(Reuse.Singleton);
            c.Register<BackupFileTypesViewModel>(Reuse.Singleton);
            c.Register<BackupViewModelFactory>(Reuse.Singleton);

            c.UseInstance(ScarletCommandBuilder.Default);
            c.UseInstance(ScarletDispatcher.Default);
            c.UseInstance(ScarletMessenger.Default);
            c.UseInstance(ScarletCommandManager.Default);
            c.UseInstance(ScarletMessageProxy.Default);
            c.UseInstance(ScarletWeakEventManager.Default);
            c.UseInstance(ScarletExitService.Default);

            return c;
        }
    }
}
