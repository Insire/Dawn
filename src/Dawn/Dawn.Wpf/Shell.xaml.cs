using AdonisUI;
using AdonisUI.Controls;
using Jot;
using Serilog;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dawn.Wpf
{
    [SupportedOSPlatform("windows7.0")]
    public partial class Shell
    {
        public bool IsLightTheme
        {
            get { return (bool)GetValue(IsLightThemeProperty); }
            set { SetValue(IsLightThemeProperty, value); }
        }

        public static readonly DependencyProperty IsLightThemeProperty = DependencyProperty.Register(
            nameof(IsLightTheme),
            typeof(bool),
            typeof(Shell),
            new PropertyMetadata(false));

        public Visibility StagingVisibility
        {
            get { return (Visibility)GetValue(StagingVisibilityProperty); }
            set { SetValue(StagingVisibilityProperty, value); }
        }

        public static readonly DependencyProperty StagingVisibilityProperty = DependencyProperty.Register(
            nameof(StagingVisibility),
            typeof(Visibility),
            typeof(Shell),
            new PropertyMetadata(Visibility.Collapsed, OnStagingVisibilityChanged));

        private static void OnStagingVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Shell shell && e.NewValue is Visibility visibility)
            {
                switch (visibility)
                {
                    case Visibility.Visible:
                        shell.SetCurrentValue(StagingCheckedProperty, true);
                        break;

                    case Visibility.Collapsed:
                    case Visibility.Hidden:
                        shell.SetCurrentValue(StagingCheckedProperty, false);
                        break;
                }
            }
        }

        public bool StagingChecked
        {
            get { return (bool)GetValue(StagingCheckedProperty); }
            set { SetValue(StagingCheckedProperty, value); }
        }

        public static readonly DependencyProperty StagingCheckedProperty = DependencyProperty.Register(
            nameof(StagingChecked),
            typeof(bool),
            typeof(Shell),
            new PropertyMetadata(false, StagingCheckedChanged));

        private static void StagingCheckedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Shell shell && e.NewValue is bool flag)
            {
                switch (flag)
                {
                    case true:
                        shell.SetCurrentValue(StagingVisibilityProperty, Visibility.Visible);
                        break;

                    case false:
                        shell.SetCurrentValue(StagingVisibilityProperty, Visibility.Collapsed);
                        break;
                }
            }
        }

        private readonly ShellViewModel _shellViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly AboutViewModel _aboutViewModel;
        private readonly ConfigurationService _configurationService;
        private readonly ILogger _log;

        public Shell(ShellViewModel shellViewModel, Tracker tracker, LogViewModel logViewModel, AboutViewModel aboutViewModel, ConfigurationService configurationService, ILogger log)
        {
            if (tracker is null)
            {
                throw new ArgumentNullException(nameof(tracker));
            }

            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            DataContext = _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));

            InitializeComponent();

            tracker.Track(this);

            _shellViewModel.Stagings.OnApplyingStagings += ShowLog;
            _shellViewModel.Updates.OnDeleting += ShowLog;
            _shellViewModel.Updates.OnDeletingAll += ShowLog;
            _shellViewModel.Updates.OnRestoring += ShowLog;
            _shellViewModel.ShowLogAction += ShowLog;
            _shellViewModel.Updates.OnMetaDataEditing += ShowEditDialog;

            _shellViewModel.Updates.OnDeleteRequested = () => Dispatcher.Invoke(() => AdonisUI.Controls.MessageBox.Show(this, new MessageBoxModel
            {
                IsSoundEnabled = false,
                Text = "This will delete all files in this backup folder. \r\nThis can not be undone.",
                Caption = "Are you sure?",
                Icon = AdonisUI.Controls.MessageBoxImage.Warning,
                Buttons = MessageBoxButtons.YesNo(),
            })) == AdonisUI.Controls.MessageBoxResult.Yes;

            _shellViewModel.Updates.OnDeleteAllRequested = () => Dispatcher.Invoke(() => AdonisUI.Controls.MessageBox.Show(this, new MessageBoxModel
            {
                IsSoundEnabled = false,
                Text = "This will delete every backup. \r\nThis can not be undone.",
                Caption = "Are you really sure?",
                Icon = AdonisUI.Controls.MessageBoxImage.Stop,
                Buttons = MessageBoxButtons.YesNo(),
            })) == AdonisUI.Controls.MessageBoxResult.Yes;

            _shellViewModel.OnApplicationUpdated = () => Dispatcher.Invoke(() => AdonisUI.Controls.MessageBox.Show(this, new MessageBoxModel
            {
                IsSoundEnabled = false,
                Text = "Your update has been prepared. \r\nDo you want to restart Dawn?",
                Caption = "Updates have been downloaded successfully.",
                Icon = AdonisUI.Controls.MessageBoxImage.Information,
                Buttons = MessageBoxButtons.YesNo(),
            })) == AdonisUI.Controls.MessageBoxResult.Yes;

            _shellViewModel.Stagings.OnEmptyDirectoryCreated = () => Dispatcher.Invoke(() => AdonisUI.Controls.MessageBox.Show(this, new MessageBoxModel
            {
                IsSoundEnabled = false,
                Text = "Applying your files didnt result in a new backup. Delete empty backup folder?",
                Caption = "Delete empty backup folder?.",
                Icon = AdonisUI.Controls.MessageBoxImage.Information,
                Buttons = MessageBoxButtons.YesNo(),
            })) == AdonisUI.Controls.MessageBoxResult.Yes;

            SetImage();
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var data = e.Data.GetData(DataFormats.FileDrop);

                if (data is string[] fileArray)
                {
                    await _shellViewModel.Stagings.Add(fileArray).ConfigureAwait(false);
                }
                else
                {
                    _log.Warning("Drop data type is unexpected");
                    _log.Warning("Drop data type found: {Format}", data.GetType().FullName);
                }
            }
            else
            {
                _log.Warning("Drop data format is unsupported or not convertible");
                foreach (var format in e.Data.GetFormats())
                {
                    _log.Warning("Drop data format found: {Format}", format);
                }
            }
        }

        private void OpenConfiguration(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var dlg = new ConfigurationWindow(_shellViewModel.Configuration)
                {
                    Owner = this
                };

                dlg.ShowDialog();
            });
        }

        private void ShowLog()
        {
            Dispatcher.Invoke(() =>
            {
                var dlg = new LoggingWindow(_logViewModel)
                {
                    Owner = this
                };

                dlg.ShowDialog();
            });
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var dlg = new AboutWindow(_aboutViewModel)
                {
                    Owner = this
                };

                dlg.ShowDialog();
            });
        }

        private BackupViewModel ShowEditDialog(BackupViewModel backupViewModel)
        {
            Dispatcher.Invoke(() =>
            {
                var dlg = new EditBackupWindow(backupViewModel)
                {
                    Owner = this
                };

                dlg.ShowDialog();
            });

            return backupViewModel;
        }

        private void SetImage()
        {
            var size = new Size(36, 36);
            var ressource = Application.Current.FindResource("dawnDrawingImage");
            if (ressource is not DrawingImage drawingImage)
            {
                return;
            }

            var image = new Image { Source = drawingImage };
            image.Measure(size);
            image.Arrange(new Rect(size));

            var rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(image);

            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));

            using (var memory = new MemoryStream())
            {
                png.Save(memory);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memory;
                bitmapImage.UriSource = null;
                bitmapImage.EndInit();

                bitmapImage.Freeze();

                SetCurrentValue(IconProperty, bitmapImage);
            }
        }

        private void OnToggleTheme(object sender, RoutedEventArgs e)
        {
            var model = _configurationService.Get();

            var isLightTheme = !IsLightTheme;

            SetValue(IsLightThemeProperty, isLightTheme);
            model.IsLightTheme = isLightTheme;

            ResourceLocator.SetColorScheme(Application.Current.Resources, isLightTheme ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);

            _configurationService.Save();

            SetImage();
        }
    }
}
