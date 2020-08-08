using Jot;
using System;
using System.Windows;

namespace Dawn.Wpf
{
    public partial class Shell
    {
        private readonly ShellViewModel _shellViewModel;

        public Shell(ShellViewModel shellViewModel, Tracker tracker)
        {
            if (tracker is null)
            {
                throw new ArgumentNullException(nameof(tracker));
            }

            DataContext = _shellViewModel = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));

            InitializeComponent();

            tracker.Track(this);
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
    }
}
