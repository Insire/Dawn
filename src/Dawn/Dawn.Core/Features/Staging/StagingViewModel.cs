namespace Dawn.Core.Features.Staging
{
    /// <summary>
    /// a file that will be copied to a folder
    /// </summary>
    public sealed class StagingViewModel : FileSystemViewModel
    {
        public StagingViewModel(string fullPath)
              : base(fullPath, true)
        {
        }
    }
}
