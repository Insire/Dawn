using CommunityToolkit.Mvvm.Input;
using Dawn.Core.Features.Backups;
using SukiUI.Controls;
using SukiUI.Dialogs;
using System;
using System.Windows.Input;

namespace Dawn.Avalonia.Features.Backups
{
    public partial class EditBackupWindow : SukiWindow
    {
        private readonly BackupViewModel _backupViewModel;

        public ICommand CloseCommand { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public EditBackupWindow()
        {
            InitializeComponent();
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public EditBackupWindow(
            BackupViewModel backupViewModel,
            ISukiDialogManager dialogManager)
        {
            DataContext = _backupViewModel = backupViewModel ?? throw new ArgumentNullException(nameof(backupViewModel));
            CloseCommand = new RelayCommand(CloseInternal, CanClose);

            InitializeComponent();

            DialogHost.Manager = dialogManager;
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
