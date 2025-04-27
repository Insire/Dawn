using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Dawn.Core.Features.Configuration;
using Dawn.Core.Features.Filesystem;
using Dawn.Core.Features.Util;
using SukiUI.Controls;
using SukiUI.Dialogs;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Dawn.Avalonia.Features
{
    public partial class ConfigurationWindow : SukiWindow
    {
        private readonly IClipboardService _clipboardService;
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly IFileSystem _fileSystem;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public ConfigurationWindow()
        {
            InitializeComponent();
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public ConfigurationWindow(
            ConfigurationViewModel configurationViewModel,
            IFileSystem fileSystem,
            IClipboardService clipboardService,
            ISukiDialogManager dialogManager)
        {
            DataContext = _configurationViewModel = configurationViewModel ?? throw new ArgumentNullException(nameof(configurationViewModel));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _clipboardService = clipboardService;

            InitializeComponent();

            DialogHost.Manager = dialogManager;
        }

        [RelayCommand]
        private async Task CopyToClipboard()
        {
            var json = JsonSerializer.Serialize(_configurationViewModel.Model);
            var bytes = Encoding.UTF8.GetBytes(json);
            var base64 = Convert.ToBase64String(bytes);

            await _clipboardService.SetDataAsync(base64);
        }

        [RelayCommand(CanExecute = nameof(CanClose))]
        private new void Close()
        {
            _configurationViewModel.Validate();
            if (_configurationViewModel.HasErrors)
            {
                return;
            }

            base.Close();
        }

        private bool CanClose()
        {
            return !_configurationViewModel.HasErrors;
        }

        private async void SelectTargetFolder(object sender, RoutedEventArgs e)
        {
            var folder = await _fileSystem.TrySelectFolderAsync();
            if (folder is not null)
            {
                _configurationViewModel.DeploymentFolder = folder;
            }
        }

        private async void SelectBackupFolder(object sender, RoutedEventArgs e)
        {
            var folder = await _fileSystem.TrySelectFolderAsync();
            if (folder is not null)
            {
                _configurationViewModel.BackupFolder = folder;
            }
        }
    }
}
