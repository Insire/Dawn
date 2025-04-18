using JetBrains.Annotations;

namespace Dawn.Core.Features.Filesystem
{
    public class FileSystemViewModel : ObservableObject
    {
        private string _fullPath;
        public string FullPath
        {
            get { return _fullPath; }
            private set { SetProperty(ref _fullPath, value); }
        }

        private string? _name;
        public string? Name
        {
            get { return _name; }
            private set { SetProperty(ref _name, value); }
        }

        private bool _isFile;
        public bool IsFile
        {
            get { return _isFile; }
            private set { SetProperty(ref _isFile, value); }
        }

        private bool _isFolder;
        [UsedImplicitly]
        public bool IsFolder
        {
            get { return _isFolder; }
            private set { SetProperty(ref _isFolder, value); }
        }

        public FileSystemViewModel(string fullPath, bool isFile)
        {
            _fullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            _isFile = isFile;
            _isFolder = !isFile;

            _name = isFile
                ? Path.GetFileName(fullPath)
                : Path.GetDirectoryName(fullPath);
        }
    }
}
