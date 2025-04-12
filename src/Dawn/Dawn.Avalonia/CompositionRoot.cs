using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Messaging;
using Dawn.Avalonia.Infrastructure;
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
        public static IContainer Get(IClassicDesktopStyleApplicationLifetime lifetime)
        {
            var c = new Container();

            var logConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Verbose)
                .Enrich.FromLogContext();

            c.Use(lifetime);
            c.Use(SynchronizationContext.Current);
            c.Use<ILogger>(logConfiguration.CreateLogger());
            c.Use(Assembly.GetAssembly(typeof(CompositionRoot)));
            c.Use(new HttpClient());
            c.Use(Process.GetCurrentProcess());

            c.Register<ConfigurationService>(Reuse.Singleton);
            c.Register(made: Made.Of(_ => ServiceInfo.Of<ConfigurationService>(), f => f.Get()));

            c.Register<IFileSystem, FileSystem>(Reuse.Singleton);
            c.Register<IFileDialogs, FileDialogs>(Reuse.Singleton);
            c.Register<LogViewModel>(Reuse.Singleton);
            c.Register<ShellViewModel>(Reuse.Singleton);
            c.Register<AboutViewModel>(Reuse.Singleton);
            c.Register<ConfigurationViewModel>(Reuse.Singleton);
            c.Register<StagingsViewModel>(Reuse.Singleton);
            c.Register<BackupsViewModel>(Reuse.Singleton);
            c.Register<BackupFileTypesViewModel>(Reuse.Singleton);
            c.Register<BackupViewModelFactory>(Reuse.Singleton);

            c.Register<ChangeDetectionViewModel>(Reuse.Singleton);
            c.Register<ChangeDetectionService>(Reuse.Singleton);

            c.Register<IScarletExceptionHandler, GlobalCommandExceptionHandler>(Reuse.Singleton);

            c.Register<Shell>(Reuse.Singleton, made: Made.Of(() => new Shell(Arg.Of<ShellViewModel>(), Arg.Of<LogViewModel>(), Arg.Of<AboutViewModel>(), Arg.Of<ChangeDetectionViewModel>(), Arg.Of<ConfigurationService>(), Arg.Of<IFileSystem>(), Arg.Of<IScarletDispatcher>(), Arg.Of<IClipboardService>())));
            c.Register<IClipboardService, ClipboardService>();

            c.Use(ScarletCommandBuilder.Default);
            c.Use(ScarletDispatcher.Default);
            c.Use(ScarletCommandManager.Default);
            c.Use(WeakReferenceMessenger.Default);
            c.Use(ScarletExitService.Default);
            c.Use(ScarletWeakEventManager.Default);

            return c;
        }
    }
}
