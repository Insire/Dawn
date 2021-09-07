namespace Dawn.Wpf
{
    public class DirectoryViewModel : FileSystemViewModel
    {
        public DirectoryViewModel(string fullPath)
           : base(fullPath, false)
        {
        }
    }
}
