using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Dawn.Core.Features.Util;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dawn.Avalonia.Infrastructure
{
    public sealed class FileDialogs(IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime)
        : IFileDialogs
    {
        private readonly IClassicDesktopStyleApplicationLifetime _classicDesktopStyleApplicationLifetime = classicDesktopStyleApplicationLifetime;

        public async Task<IReadOnlyList<string>?> TrySelectFilesAsync()
        {
            var window = _classicDesktopStyleApplicationLifetime.MainWindow;
            if (window is null)
            {
                return null;
            }

            var options = new FilePickerOpenOptions { Title = "Select files", AllowMultiple = true };
            var files = await window.StorageProvider.OpenFilePickerAsync(options);

            return files.Select(p => p.Path.AbsolutePath).ToArray();
        }

        public async Task<string?> TrySelectFolderAsync()
        {
            var window = _classicDesktopStyleApplicationLifetime.MainWindow;
            if (window is null)
            {
                return null;
            }

            var options = new FolderPickerOpenOptions { Title = "Select folder", AllowMultiple = false };
            var folders = await window.StorageProvider.OpenFolderPickerAsync(options);

            var folder = folders.FirstOrDefault();

            return folder?.Path.AbsolutePath;
        }
    }
}
