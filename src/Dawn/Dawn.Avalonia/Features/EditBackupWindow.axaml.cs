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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public EditBackupWindow()
        {
            InitializeComponent();
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

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
