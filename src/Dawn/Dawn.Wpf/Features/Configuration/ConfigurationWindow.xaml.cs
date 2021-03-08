using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using System;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public sealed partial class ConfigurationWindow
    {
        private readonly ConfigurationViewModel _configurationViewModel;

        public ICommand CloseCommand { get; }
        public ICommand CopyToClipboardCommand { get; }

        public ConfigurationWindow(ConfigurationViewModel configurationViewModel)
        {
            DataContext = _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));

            CloseCommand = new RelayCommand(CloseInternal, CanClose);
            CopyToClipboardCommand = new RelayCommand(CopyToClipboard);

            InitializeComponent();
        }

        private void CopyToClipboard()
        {
            var json = JsonConvert.SerializeObject(_configurationViewModel.Model, Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            var base64 = Convert.ToBase64String(bytes);

            Clipboard.SetDataObject($"json='{base64}'");
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
