using Dawn.Core.Features.Logging;

namespace Dawn.Avalonia.Features;

public sealed class FileDialogs:IFileDialogs
{
    public bool TrySelectFiles(out string[] files)
    {
        files = null;
        return false;
    }

    public bool TrySelectFolder(out string folder)
    {
        folder = null;
        return false;
    }
}
