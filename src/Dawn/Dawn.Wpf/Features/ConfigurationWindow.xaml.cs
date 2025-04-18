using CommunityToolkit.Mvvm.Input;
using Dawn.Core.Features.Configuration;
using Dawn.Core.Features.Filesystem;
using Dawn.Core.Features.Util;
using System;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Dawn.Wpf
{
    [SupportedOSPlatform("windows7.0")]
    public sealed partial class ConfigurationWindow
    {
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly IFileSystem _fileSystem;
        private readonly IClipboardService _clipboardService;

        public ICommand CloseCommand { get; }
        public ICommand CopyToClipboardCommand { get; }

        public ConfigurationWindow(ConfigurationViewModel configurationViewModel, IFileSystem fileSystem, IClipboardService clipboardService)
        {
            DataContext = _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _clipboardService = clipboardService;
            CloseCommand = new RelayCommand(CloseInternal, CanClose);
            CopyToClipboardCommand = new AsyncRelayCommand(CopyToClipboard);

            InitializeComponent();
        }

        private async Task CopyToClipboard()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_configurationViewModel.Model);
            var bytes = Encoding.UTF8.GetBytes(json);
            var base64 = Convert.ToBase64String(bytes);

            await _clipboardService.SetDataAsync(base64);
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
            if (_fileSystem.TrySelectFolder(out var folder) && !string.IsNullOrEmpty(folder))
            {
                _configurationViewModel.DeploymentFolder = folder;
            }
        }

        private void SelectBackupFolder(object sender, RoutedEventArgs e)
        {
            if (_fileSystem.TrySelectFolder(out var folder) && !string.IsNullOrEmpty(folder))
            {
                _configurationViewModel.BackupFolder = folder;
            }
        }
    }
}
