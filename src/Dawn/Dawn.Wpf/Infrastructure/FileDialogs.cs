using Dawn.Core.Features.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dawn.Wpf
{
    public sealed class FileDialogs : IFileDialogs
    {
        public Task<IReadOnlyList<string>?> TrySelectFilesAsync()
        {
            var dlg = new OpenFileDialog
            {
                ClientGuid = Guid.Parse("7b75d7d5-005a-44c1-a9f4-73b520517189"),
                CheckFileExists = true,
                CheckPathExists = true,
                DereferenceLinks = true,
                AddExtension = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Multiselect = true,
                Title = "Select files",
                ValidateNames = true
            };

            var result = dlg.ShowDialog();

            return result == true
                ? Task.FromResult<IReadOnlyList<string>?>(dlg.FileNames)
                : Task.FromResult<IReadOnlyList<string>?>(null);
        }

        public Task<string?> TrySelectFolderAsync()
        {
            var dlg = new OpenFolderDialog
            {
                ClientGuid = Guid.Parse("7b75d7d5-005a-44c1-a9f4-73b520517189"),
                Title = "Select a folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Multiselect = false,
                AddToRecent = false
            };

            var result = dlg.ShowDialog();
            return result == true
                ? Task.FromResult<string?>(dlg.FolderName)
                : Task.FromResult<string?>(null);
        }
    }
}
