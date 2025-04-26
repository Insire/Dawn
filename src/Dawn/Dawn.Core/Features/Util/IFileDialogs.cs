namespace Dawn.Core.Features.Util
{
    public interface IFileDialogs
    {
        Task<IReadOnlyList<string>?> TrySelectFilesAsync();

        Task<string?> TrySelectFolderAsync();
    }
}
