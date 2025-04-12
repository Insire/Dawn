using Avalonia.Input.Platform;
using Dawn.Core.Features.Util;
using System.Threading.Tasks;

namespace Dawn.Avalonia.Features;

public sealed class ClipbboardService:IClipboardService
{
    private readonly IClipboard? _clipboard;

    public ClipbboardService(IClipboard? clipboard)
    {
        _clipboard = clipboard;
    }

    public async Task SetData(string text)
    {
        if (_clipboard is null)
        {
            return;
        }

        await _clipboard.SetTextAsync(text);
    }
}
