namespace Dawn.Core.Features.Util
{
    public interface IFileDialogs
    {
        bool TrySelectFiles(out string[]? files);

        bool TrySelectFolder(out string? folder);
    }
}
