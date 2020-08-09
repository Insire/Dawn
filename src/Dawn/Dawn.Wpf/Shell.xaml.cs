using Jot;
using System;
using System.Windows;

namespace Dawn.Wpf
{
    public partial class Shell
    {
        private readonly ShellViewModel _shellViewModel;
        private readonly LogViewModel _logViewModel;

        public Shell(ShellViewModel shellViewModel, Tracker tracker, LogViewModel logViewModel)
        {
            if (tracker is null)
            {
                throw new ArgumentNullException(nameof(tracker));
            }

            _logViewModel = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));

            DataContext = _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));

            InitializeComponent();

            tracker.Track(this);

            _shellViewModel.Stagings.OnApplyingStagings += OnApplyingStagings;
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

        private void OnApplyingStagings()
        {
            var dlg = new LoggingWindow(_logViewModel)
            {
                Owner = this
            };

            dlg.ShowDialog();
        }
    }
}
