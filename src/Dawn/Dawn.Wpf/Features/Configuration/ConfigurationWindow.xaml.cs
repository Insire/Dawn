using Ookii.Dialogs.Wpf;
using System;
using System.Windows;

namespace Dawn.Wpf
{
    public sealed partial class ConfigurationWindow
    {
        private readonly ConfigurationViewModel _configurationViewModel;

        public ConfigurationWindow(ConfigurationViewModel configurationViewModel)
        {
            DataContext = _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));

            InitializeComponent();
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
            var folder = _configurationViewModel.TargetFolder;
            if (TrySelectFolder(out folder))
            {
                _configurationViewModel.TargetFolder = folder;
            }
        }

        private void SelectBackupFolder(object sender, RoutedEventArgs e)
        {
            var folder = _configurationViewModel.BackupFolder;
            if (TrySelectFolder(out folder))
            {
                _configurationViewModel.BackupFolder = folder;
            }
        }
    }
}
