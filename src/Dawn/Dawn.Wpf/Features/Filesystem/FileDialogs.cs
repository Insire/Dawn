using Dawn.Core.Features.Logging;

namespace Dawn.Wpf;

public class FileDialogs:IFileDialogs
{
    public bool TrySelectFiles(out string[] files)
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
            ValidateNames = true,
        };

        var result = dlg.ShowDialog();

        if (result == true)
        {
            files = dlg.FileNames;
        }
        else
        {
            files = null;
        }

        return result ?? false;
    }

    public bool TrySelectFolder(out string folder)
    {

        var dlg = new OpenFolderDialog
        {
            ClientGuid = Guid.Parse("7b75d7d5-005a-44c1-a9f4-73b520517189"),
            Title = "Select a folder",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Multiselect = false,
            AddToRecent = false,
        };

        var result = dlg.ShowDialog();

        if (result == true)
        {
            folder = dlg.FolderName;
        }
        else
        {
            folder = null;
        }

        return result ?? false;
    }
}
