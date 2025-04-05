namespace Dawn.Core.Features.Filesystem
{
    public class DirectoryViewModel : FileSystemViewModel
    {
        public DirectoryViewModel(string fullPath)
           : base(fullPath, false)
        {
        }
    }
}
