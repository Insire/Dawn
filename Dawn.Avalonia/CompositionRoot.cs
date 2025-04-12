using CommunityToolkit.Mvvm.Messaging;
using Dawn.Avalonia.Features;
using Dawn.Core;
using Dawn.Core.Features.About;
using Dawn.Core.Features.Backups;
using Dawn.Core.Features.ChangeDetection;
using Dawn.Core.Features.Configuration;
using Dawn.Core.Features.Filesystem;
using Dawn.Core.Features.Logging;
using Dawn.Core.Features.Staging;
using Dawn.Core.Features.Util;
using DryIoc;
using MvvmScarletToolkit;
using Serilog;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using LogEventLevel = Serilog.Events.LogEventLevel;

namespace Dawn.Avalonia
{
    internal static class CompositionRoot
    {
        public static IContainer Get()
        {
            var c = new Container();

            var logViewModel = new LogViewModel(ScarletCommandBuilder.Default, SynchronizationContext.Current);

            var logConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Verbose)
                .Enrich.FromLogContext();

            c.Use(SynchronizationContext.Current);
            c.Use<ILogger>(logConfiguration.CreateLogger());
            c.Use(logViewModel);
            c.Use(Assembly.GetAssembly(typeof(CompositionRoot)));
            c.Use(new HttpClient());
            c.Use(Process.GetCurrentProcess());

            c.Register<Shell>(Reuse.Singleton);

            c.Register<IFileSystem, FileSystem>(Reuse.Singleton);
            c.Register<ShellViewModel>(Reuse.Singleton);
            c.Register<AboutViewModel>(Reuse.Singleton);
            c.Register<ConfigurationViewModel>(Reuse.Singleton);
            c.Register<StagingsViewModel>(Reuse.Singleton);
            c.Register<BackupsViewModel>(Reuse.Singleton);
            c.Register<BackupFileTypesViewModel>(Reuse.Singleton);
            c.Register<BackupViewModelFactory>(Reuse.Singleton);
            c.Register<IFileDialogs, FileDialogs>(Reuse.Singleton);

            c.Register<ChangeDetectionViewModel>(Reuse.Singleton);
            c.Register<ChangeDetectionService>(Reuse.Singleton);

            c.Use(WeakReferenceMessenger.Default);

            c.Register<IScarletExceptionHandler, GlobalCommandExceptionHandler>(Reuse.Singleton);

            c.Register<Shell>(Reuse.Singleton);
            c.Register<IClipboardService, ClipbboardService>();

            c.Register(made: Made.Of(_ => ServiceInfo.Of<Shell>(), f => f.Clipboard));

            return c;
        }
    }
}
