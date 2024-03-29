using DryIoc;
using Jot;
using Jot.Storage;
using CommunityToolkit.Mvvm.Messaging;
using MvvmScarletToolkit;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace Dawn.Wpf
{
    internal static class CompositionRoot
    {
        public static IContainer Get()
        {
            var c = new Container();

            var logViewModel = new LogViewModel(ScarletCommandBuilder.Default, SynchronizationContext.Current);

            var logConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                .WriteTo.Async(c => c.File("./logs/log.txt", buffered: true)
                    .WriteTo.Logger(lc => lc.Filter
                                .ByIncludingOnly((o) => Matching.FromSource<StagingsViewModel>().Invoke(o)
                                                    || Matching.FromSource<BackupsViewModel>().Invoke(o)
                                                    || Matching.FromSource<BackupViewModel>().Invoke(o)
                                                    || Matching.FromSource<ShellViewModel>().Invoke(o))
                                .WriteTo.Sink(logViewModel, LogEventLevel.Verbose)));

            c.Use(SynchronizationContext.Current);
            c.Use<ILogger>(logConfiguration.CreateLogger());
            c.Use(logViewModel);
            c.Use(Assembly.GetAssembly(typeof(CompositionRoot)));
            c.Use(new HttpClient());
            c.Use(Process.GetCurrentProcess());

            c.Register<Shell>(Reuse.Singleton);
            var tracker = new Tracker(new JsonFileStore(Environment.SpecialFolder.CommonApplicationData));
            tracker.Configure<Shell>()
                .Id(_ => $"[Width={SystemParameters.VirtualScreenWidth},Height{SystemParameters.VirtualScreenHeight}]")
                .Properties(w => new { w.Height, w.Width, w.Left, w.Top, w.WindowState })
                .PersistOn(nameof(Window.Closing))
                .StopTrackingOn(nameof(Window.Closing));
            c.Use(tracker);

            c.Register<ConfigurationService>(Reuse.Singleton);
            c.Register(made: Made.Of(_ => ServiceInfo.Of<ConfigurationService>(), f => f.Get()));

            c.Register<IFileSystem, FileSystem>(Reuse.Singleton);
            c.Register<ShellViewModel>(Reuse.Singleton);
            c.Register<AboutViewModel>(Reuse.Singleton);
            c.Register<ConfigurationViewModel>(Reuse.Singleton);
            c.Register<StagingsViewModel>(Reuse.Singleton);
            c.Register<BackupsViewModel>(Reuse.Singleton);
            c.Register<BackupFileTypesViewModel>(Reuse.Singleton);
            c.Register<BackupViewModelFactory>(Reuse.Singleton);

            c.Register<ChangeDetectionViewModel>(Reuse.Singleton);
            c.Register<ChangeDetectionService>(Reuse.Singleton);

            c.Use(ScarletCommandBuilder.Default);
            c.Use(ScarletDispatcher.Default);
            c.Use(ScarletCommandManager.Default);
            c.Use(WeakReferenceMessenger.Default);
            c.Use(ScarletExitService.Default);
            c.Use(ScarletWeakEventManager.Default);

            c.Register<IScarletExceptionHandler, GlobalCommandExceptionHandler>(Reuse.Singleton);

            return c;
        }
    }
}
