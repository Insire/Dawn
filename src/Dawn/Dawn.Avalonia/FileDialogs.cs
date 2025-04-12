using Dawn.Core.Features.Util;

namespace Dawn.Avalonia
{
    public sealed class FileDialogs : IFileDialogs
    {
        public bool TrySelectFiles(out string[]? files)
        {
            files = null;
            return false;
        }

        public bool TrySelectFolder(out string? folder)
        {
            folder = null;
            return false;
        }
    }
}
