using Avalonia.Controls;
using Avalonia.Interactivity;
using Dawn.Core.Features.Backups;

namespace Dawn.Avalonia.Features.Backups
{
    public partial class BackupView : UserControl
    {
        public BackupView()
        {
            AddHandler(LoadedEvent, OnLoaded);

            InitializeComponent();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is BackupViewModel backupViewModel)
            {
                backupViewModel.LoadMetaDataCommand.Execute(null);
            }
        }
    }
}
