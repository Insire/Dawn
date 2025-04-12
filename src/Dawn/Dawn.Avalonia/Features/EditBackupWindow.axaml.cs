using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Dawn.Core.Features.Backups;
using System;
using System.Windows.Input;

namespace Dawn.Avalonia.Features
{
    public partial class EditBackupWindow : Window
    {
        private readonly BackupViewModel _backupViewModel;

        public ICommand CloseCommand { get; }

        public EditBackupWindow()
        {
            InitializeComponent();
        }

        public EditBackupWindow(BackupViewModel backupViewModel)
        {
            DataContext = _backupViewModel = backupViewModel ?? throw new ArgumentNullException(nameof(backupViewModel));
            CloseCommand = new RelayCommand(CloseInternal, CanClose);

            InitializeComponent();
        }

        private void CloseInternal()
        {
            Close();
        }

        private bool CanClose()
        {
            return !_backupViewModel.IsBusy;
        }
    }
}
