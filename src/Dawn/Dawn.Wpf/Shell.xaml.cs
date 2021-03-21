using AdonisUI;
using AdonisUI.Controls;
using Jot;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dawn.Wpf
{
    public partial class Shell
    {
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
            if (sender is Shell shell)
            {
                if (e.NewValue is Visibility visibility)
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
            if (sender is Shell shell)
            {
                if (e.NewValue is bool flag)
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
        }

        private readonly ShellViewModel _shellViewModel;
        private readonly LogViewModel _logViewModel;
        private readonly AboutViewModel _aboutViewModel;
        private readonly ConfigurationService _configurationService;

        public Shell(ShellViewModel shellViewModel, Tracker tracker, LogViewModel logViewModel, AboutViewModel aboutViewModel, ConfigurationService configurationService)
        {
            if (tracker is null)
            {
                throw new ArgumentNullException(nameof(tracker));
            }

            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));
            _aboutViewModel = aboutViewModel ?? throw new ArgumentNullException(nameof(aboutViewModel));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            DataContext = _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));

            InitializeComponent();

            tracker.Track(this);

            _shellViewModel.Stagings.OnApplyingStagings += ShowLog;
            _shellViewModel.Updates.OnDeleting += ShowLog;
            _shellViewModel.Updates.OnDeletingAll += ShowLog;
            _shellViewModel.Updates.OnRestoring += ShowLog;
            _shellViewModel.ShowLogAction += ShowLog;
            _shellViewModel.Updates.OnMetaDataEditing += ShowEditDialog;

            _shellViewModel.Updates.OnDeleteRequested = () => AdonisUI.Controls.MessageBox.Show(this, new MessageBoxModel
            {
                IsSoundEnabled = false,
                Text = "This will delete all files in this backup folder. \r\nThis can not be undone.",
                Caption = "Are you sure?",
                Icon = AdonisUI.Controls.MessageBoxImage.Warning,
                Buttons = MessageBoxButtons.YesNo(),
            }) == AdonisUI.Controls.MessageBoxResult.Yes;

            _shellViewModel.Updates.OnDeleteAllRequested = () => AdonisUI.Controls.MessageBox.Show(this, new MessageBoxModel
            {
                IsSoundEnabled = false,
                Text = "This will delete every backup. \r\nThis can not be undone.",
                Caption = "Are you really sure?",
                Icon = AdonisUI.Controls.MessageBoxImage.Stop,
                Buttons = MessageBoxButtons.YesNo(),
            }) == AdonisUI.Controls.MessageBoxResult.Yes;

            _shellViewModel.OnApplicationUpdated = () => AdonisUI.Controls.MessageBox.Show(this, new MessageBoxModel
            {
                IsSoundEnabled = false,
                Text = "Your update has been prepared. \r\nDo you want to restart Dawn?",
                Caption = "Updates have been downloaded successfully.",
                Icon = AdonisUI.Controls.MessageBoxImage.Information,
                Buttons = MessageBoxButtons.YesNo(),
            }) == AdonisUI.Controls.MessageBoxResult.Yes;

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
            }
        }

        private void OpenConfiguration(object sender, RoutedEventArgs e)
        {
            var dlg = new ConfigurationWindow(_shellViewModel.Configuration)
            {
                Owner = this
            };

            dlg.ShowDialog();
        }

        private void ShowLog()
        {
            var dlg = new LoggingWindow(_logViewModel)
            {
                Owner = this
            };

            dlg.ShowDialog();
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            var dlg = new AboutWindow(_aboutViewModel)
            {
                Owner = this
            };

            dlg.ShowDialog();
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
            if (!(ressource is DrawingImage drawingImage))
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
            model.IsLightTheme = !model.IsLightTheme;

            ResourceLocator.SetColorScheme(Application.Current.Resources, model.IsLightTheme ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);

            _configurationService.Save();

            SetImage();
        }
    }
}
