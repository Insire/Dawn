using Microsoft.Toolkit.Mvvm.Input;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Dawn.Wpf
{
    [SupportedOSPlatform("windows7.0")]
    public sealed partial class ConfigurationWindow
    {
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _log;

        public ICommand CloseCommand { get; }
        public ICommand CopyToClipboardCommand { get; }

        public ConfigurationWindow(ConfigurationViewModel configurationViewModel, IFileSystem fileSystem, ILogger log)
        {
            DataContext = _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            CloseCommand = new RelayCommand(CloseInternal, CanClose);
            CopyToClipboardCommand = new RelayCommand(CopyToClipboard);

            InitializeComponent();
        }

        private void CopyToClipboard()
        {
            var json = JsonConvert.SerializeObject(_configurationViewModel.Model, Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            var base64 = Convert.ToBase64String(bytes);

            try
            {
                Clipboard.SetDataObject($"json='{base64}'");
            }
            catch (Exception ex)
            {
                _log.LogError(ex);
            }
        }

        private void CloseInternal()
        {
            _configurationViewModel.Validate();
            if (_configurationViewModel.HasErrors)
            {
                return;
            }

            DialogResult = true;
        }

        private bool CanClose()
        {
            return !_configurationViewModel.HasErrors;
        }

        private void SelectTargetFolder(object sender, RoutedEventArgs e)
        {
            if (_fileSystem.TrySelectFolder(out var folder))
            {
                _configurationViewModel.DeploymentFolder = folder;
            }
        }

        private void SelectBackupFolder(object sender, RoutedEventArgs e)
        {
            if (_fileSystem.TrySelectFolder(out var folder))
            {
                _configurationViewModel.BackupFolder = folder;
            }
        }
    }
}
