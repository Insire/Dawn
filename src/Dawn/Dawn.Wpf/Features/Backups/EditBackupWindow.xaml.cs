using MvvmScarletToolkit;
using MvvmScarletToolkit.Commands;
using System;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public partial class EditBackupWindow
    {
        private readonly BackupViewModel _backupViewModel;

        public ICommand CloseCommand { get; }

        public EditBackupWindow(BackupViewModel backupViewModel)
        {
            DataContext = _backupViewModel = backupViewModel ?? throw new ArgumentNullException(nameof(backupViewModel));

            CloseCommand = new RelayCommand(ScarletCommandBuilder.Default, CloseInternal, CanClose);

            InitializeComponent();
        }

        private void CloseInternal()
        {
            DialogResult = true;
        }

        private bool CanClose()
        {
            return !_backupViewModel.IsBusy;
        }
    }
}
