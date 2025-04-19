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
using Serilog.Filters;
using SukiUI.Dialogs;
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

            var clipboardService = new ClipboardService(lifetime);
            var logViewModel = new LogViewModel(ScarletCommandBuilder.Default, SynchronizationContext.Current!, clipboardService);

            var logConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                .WriteTo.Async(c => c.File("./logs/log.txt", buffered: true)
                    .WriteTo.Debug()
                    .WriteTo.Sink(logViewModel, LogEventLevel.Verbose))
                    .WriteTo.Logger(lc => lc.Filter
                                .ByIncludingOnly((o) => Matching.FromSource<StagingsViewModel>().Invoke(o)
                                                    || Matching.FromSource<BackupsViewModel>().Invoke(o)
                                                    || Matching.FromSource<BackupViewModel>().Invoke(o)
                                                    || Matching.FromSource<ShellViewModel>().Invoke(o))
                                );

            var logger = logConfiguration.CreateLogger();

            c.Use(logViewModel);
            c.Use<ILogger>(logger);

            c.Use(lifetime);
            c.Use<IClipboardService>(clipboardService);
            c.Use(SynchronizationContext.Current);
            c.Use(Assembly.GetAssembly(typeof(CompositionRoot)));
            c.Use(Process.GetCurrentProcess());

            c.Register<ConfigurationService>(Reuse.Singleton);
            c.Register(made: Made.Of(_ => ServiceInfo.Of<ConfigurationService>(), f => f.Get()));

            c.Register<IFileSystem, FileSystem>(Reuse.Singleton);
            c.Register<IFileDialogs, FileDialogs>(Reuse.Singleton);
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

            c.Register<HttpClient>(Reuse.Singleton, made: Made.Of(() => new HttpClient()));
            c.Register<Shell>(Reuse.Singleton, made: Made.Of(() => new Shell(Arg.Of<ShellViewModel>(), Arg.Of<LogViewModel>(), Arg.Of<AboutViewModel>(), Arg.Of<ChangeDetectionViewModel>(), Arg.Of<ConfigurationService>(), Arg.Of<IFileSystem>(), Arg.Of<IScarletDispatcher>(), Arg.Of<IClipboardService>(), Arg.Of<SynchronizationContext>(), Arg.Of<ISukiDialogManager>())));
            c.Register<ISukiDialogManager, SukiDialogManager>(Reuse.Singleton);

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
