using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Dawn.Avalonia.Features;
using Dawn.Avalonia.Infrastructure;
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
using SukiUI;
using SukiUI.Controls;
using SukiUI.Dialogs;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChangeDetectionWindow = Dawn.Avalonia.Features.ChangeDetection.ChangeDetectionWindow;
using EditBackupWindow = Dawn.Avalonia.Features.Backups.EditBackupWindow;

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
        private readonly ISukiDialogManager _dialogManager;

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
            SynchronizationContext context,
            ISukiDialogManager dialogManager)
        {
            _logViewModel = logViewModel;
            _aboutViewModel = aboutViewModel;
            _changeDetectionViewModel = changeDetectionViewModel;
            _configurationService = configurationService;
            _fileSystem = fileSystem;
            _dispatcher = dispatcher;
            _clipboardService = clipboardService;
            DataContext = _shellViewModel = shellViewModel;

            InitializeComponent();
            DialogHost.Manager = _dialogManager = dialogManager;

            AddHandler(DragDrop.DropEvent, OnDrop);
            AddHandler(LoadedEvent, OnLoaded);
            AddHandler(WindowClosedEvent, OnClosed);

            var subscription1 = _shellViewModel.Stagings
                .WhenPropertyChanged(p => p.IsEmpty, notifyOnInitialValue: false)
                .ObserveOn(context)
                .Subscribe(p => SetValue(StagingCheckedProperty, !p.Value));

            _shellViewModel.ShowLogAction += ShowLog;
            _shellViewModel.Stagings.OnApplyingStagings += ShowLog;
            _shellViewModel.Updates.OnDeleting += ShowLog;
            _shellViewModel.Updates.OnDeletingAll += ShowLog;
            _shellViewModel.Updates.OnRestoring += ShowLog;
            _shellViewModel.Updates.OnMetaDataEditing += ShowEditDialog;

            _shellViewModel.Updates.OnDeleteRequested = () => dialogManager.CreateDialog()
                .OfType(NotificationType.Warning)
                .WithTitle("Are you sure?")
                .WithContent("This will delete all files in this backup folder. \r\nThis can not be undone.")
                .WithYesNoResult("Yes", "No")
                .TryShowAsync();

            _shellViewModel.Updates.OnDeleteAllRequested = () => _dialogManager.CreateDialog()
                .OfType(NotificationType.Warning)
                .WithTitle("Are you really sure?")
                .WithContent("This will delete every backup. \r\nThis can not be undone.")
                .WithYesNoResult("Yes", "No")
                .TryShowAsync();

            _shellViewModel.OnApplicationUpdated = () => _dialogManager.CreateDialog()
                .OfType(NotificationType.Information)
                .WithTitle("Updates have been downloaded successfully.")
                .WithContent("Your update has been prepared. \r\nDo you want to restart Dawn?")
                .WithYesNoResult("Yes", "No")
                .TryShowAsync();

            _shellViewModel.Stagings.OnEmptyDirectoryCreated = async () =>
            {
                using var focus = new DialogHostFocus(DialogHost, _dialogManager);
                return await _dialogManager.CreateDialog()
                    .OfType(NotificationType.Warning)
                    .WithTitle("Delete empty backup folder?")
                    .WithContent("Applying your files didnt result in a new backup. Delete empty backup folder?")
                    .WithYesNoResult("Yes", "No")
                    .TryShowAsync();
            };

            _shellViewModel.Updates.OnDetectChanges = (vm) => _dispatcher.Invoke(async () =>
            {
                var wnd = new ChangeDetectionWindow(_changeDetectionViewModel, _dialogManager);

                _ = _changeDetectionViewModel.DetectChanges(vm);

                await wnd.ShowDialog(this);
            });

            var subscription2 = StagingCheckedProperty.Changed.AddClassHandler<Shell, bool>(OnStagingCheckedChanged);
            _disposables = new CompositeDisposable(subscription1, subscription2);
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
                sender.StagingDetails.SetCurrentValue(IsVisibleProperty, staging);
            }
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not ShellViewModel shellViewModel)
            {
                return;
            }

            shellViewModel.Configuration.ValidateCommand.Execute(null);
            shellViewModel.Updates.LoadCommand.Execute(null);
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

        private async void OpenConfiguration(object sender, RoutedEventArgs e)
        {
            await _dispatcher.Invoke(async () =>
            {
                var dlg = new ConfigurationWindow(_shellViewModel.Configuration, _fileSystem, _clipboardService, _dialogManager);

                await dlg.ShowDialog(this);
            });
        }

        private async void ShowLog(object sender, RoutedEventArgs e)
            => await ShowLog();

        private async Task ShowLog()
        {
            await _dispatcher.Invoke(() =>
            {
                var dlg = new Features.Logging.LoggingWindow(_logViewModel, _dialogManager);

                // don't await this dialog,
                // because we don't to wait for this dialog to close before returning control to the caller
                dlg.ShowDialog(this);
            });
        }

        private async void ShowAbout(object sender, RoutedEventArgs e)
        {
            await _dispatcher.Invoke(async () =>
            {
                var dlg = new AboutWindow(_aboutViewModel, _dialogManager);

                await dlg.ShowDialog(this);
            });
        }

        private async Task<BackupViewModel> ShowEditDialog(BackupViewModel backupViewModel)
        {
            await _dispatcher.Invoke(async () =>
            {
                var dlg = new EditBackupWindow(backupViewModel, _dialogManager);

                await dlg.ShowDialog(this);
            });

            return backupViewModel;
        }

        private void OnToggleTheme(object sender, RoutedEventArgs e)
        {
            var model = _configurationService.Get();
            var theme = SukiTheme.GetInstance();
            var isLightTheme = !(theme.ActiveBaseTheme == ThemeVariant.Light);

            model.IsLightTheme = isLightTheme;
            theme.ChangeBaseTheme(isLightTheme ? ThemeVariant.Light : ThemeVariant.Dark);
            _configurationService.Save();

            // TODO
            //SetImage();
        }

        private void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
        }
    }
}
