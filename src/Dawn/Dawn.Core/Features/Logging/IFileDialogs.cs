namespace Dawn.Core.Features.Logging
{
    public interface IFileDialogs
    {
        bool TrySelectFiles(out string[]? files);

        bool TrySelectFolder(out string? folder);
    }
}
