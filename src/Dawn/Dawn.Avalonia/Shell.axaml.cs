using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Dawn.Avalonia.Features;
using Dawn.Core;
using Dawn.Core.Features.About;
using Dawn.Core.Features.Backups;
using Dawn.Core.Features.ChangeDetection;
using Dawn.Core.Features.Configuration;
using Dawn.Core.Features.Filesystem;
using Dawn.Core.Features.Logging;
using Dawn.Core.Features.Util;
using DynamicData.Binding;
using MvvmScarletToolkit;
using Serilog;
using SukiUI.Controls;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace Dawn.Avalonia
{
    public partial class Shell : SukiWindow
    {
        private readonly ShellViewModel _shellViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly AboutViewModel _aboutViewModel;
        private readonly ChangeDetectionViewModel _changeDetectionViewModel;
        private readonly ConfigurationService _configurationService;
        private readonly IFileSystem _fileSystem;
        private readonly IScarletDispatcher _dispatcher;
        private readonly IClipboardService _clipboardService;
        private readonly CompositeDisposable _disposables;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public Shell()
        {
            InitializeComponent();
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public Shell(
            ShellViewModel shellViewModel,
            LogViewModel logViewModel,
            AboutViewModel aboutViewModel,
            ChangeDetectionViewModel changeDetectionViewModel,
            ConfigurationService configurationService,
            IFileSystem fileSystem,
            IScarletDispatcher dispatcher,
            IClipboardService clipboardService,
            SynchronizationContext context)
        {
            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
            _changeDetectionViewModel = changeDetectionViewModel ?? throw new ArgumentNullException(nameof(changeDetectionViewModel));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _dispatcher = dispatcher;
            _clipboardService = clipboardService;
            DataContext = _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));

            InitializeComponent();

            AddHandler(DragDrop.DropEvent, OnDrop);
            AddHandler(LoadedEvent, OnLoaded);
            AddHandler(WindowClosedEvent, OnClosed);

            var subscription1 = _shellViewModel.Stagings
                .WhenPropertyChanged(p => p.IsEmpty, notifyOnInitialValue: false)
                .ObserveOn(context)
                .Subscribe(p =>
                {
                    SetValue(StagingCheckedProperty, !p.Value);
                });

            _disposables = new CompositeDisposable(subscription1);

            StagingCheckedProperty.Changed.AddClassHandler<Shell, bool>(OnStagingCheckedChanged);
        }

        public static readonly StyledProperty<bool> StagingCheckedProperty =
            AvaloniaProperty.Register<Shell, bool>(nameof(StagingChecked), defaultValue: false);

        public bool StagingChecked
        {
            get => GetValue(StagingCheckedProperty);
            set => SetValue(StagingCheckedProperty, value);
        }

        private static void OnStagingCheckedChanged(Shell sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool staging)
            {
                sender.StagingDetails.SetCurrentValue(Control.IsVisibleProperty, staging);
            }
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ShellViewModel shellViewModel)
            {
                shellViewModel.Configuration.ValidateCommand.Execute(null);
                shellViewModel.Updates.LoadCommand.Execute(null);
            }
        }

        private void OnClosed(object? sender, RoutedEventArgs e)
        {
            _disposables.Dispose();
        }

        private async void OnDrop(object? sender, DragEventArgs e)
        {
            var files = e.Data.GetFiles();
            if (files == null)
            {
                return;
            }

            var filesOrFolders = files.Select(f => f.Path.LocalPath).ToArray();

            await _shellViewModel.Stagings.Add(filesOrFolders).ConfigureAwait(false);
        }

        private void OpenConfiguration(object sender, RoutedEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                var dlg = new ConfigurationWindow(_shellViewModel.Configuration, _fileSystem, _clipboardService);

                dlg.ShowDialog(this);
            });
        }

        private void ShowLog(object sender, RoutedEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                var dlg = new LoggingWindow(_logViewModel);

                dlg.ShowDialog(this);
            });
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                var dlg = new AboutWindow(_aboutViewModel);

                dlg.ShowDialog(this);
            });
        }

        private BackupViewModel ShowEditDialog(BackupViewModel backupViewModel)
        {
            _dispatcher.Invoke(() =>
            {
                var dlg = new EditBackupWindow(backupViewModel);

                dlg.ShowDialog(this);
            });

            return backupViewModel;
        }

        private void OnToggleTheme(object sender, RoutedEventArgs e)
        {
            //var model = _configurationService.Get();

            //var isLightTheme = !IsLightTheme;

            //SetValue(IsLightThemeProperty, isLightTheme);
            //model.IsLightTheme = isLightTheme;
            //ResourceLocator.SetColorScheme(Application.Current.Resources, isLightTheme ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
            //_configurationService.Save();

            //SetImage();
        }
    }
}
