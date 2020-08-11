using MvvmScarletToolkit;
using MvvmScarletToolkit.Commands;
using Ookii.Dialogs.Wpf;
using System;
using System.Windows;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public sealed partial class ConfigurationWindow
    {
        private readonly ConfigurationViewModel _configurationViewModel;

        public ICommand CloseCommand { get; }

        public ConfigurationWindow(ConfigurationViewModel configurationViewModel)
        {
            DataContext = _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));

            CloseCommand = new RelayCommand(ScarletCommandBuilder.Default, CloseInternal, CanClose);

            InitializeComponent();
        }

        private void CloseInternal()
        {
            DialogResult = true;
        }

        private bool CanClose()
        {
            return _configurationViewModel.Validation.IsValid && !_configurationViewModel.IsBusy;
        }

        public bool TrySelectFolder(out string folder)
        {
            var dlg = new VistaFolderBrowserDialog
            {
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true,
                Description = "Select a folder"
            };

            var result = dlg.ShowDialog();

            if (result == true)
            {
                folder = dlg.SelectedPath;
            }
            else
            {
                folder = null;
            }

            return result ?? false;
        }

        private void SelectTargetFolder(object sender, RoutedEventArgs e)
        {
            if (TrySelectFolder(out var folder))
            {
                _configurationViewModel.DeploymentFolder = folder;
            }
        }

        private void SelectBackupFolder(object sender, RoutedEventArgs e)
        {
            if (TrySelectFolder(out var folder))
            {
                _configurationViewModel.BackupFolder = folder;
            }
        }
    }
}