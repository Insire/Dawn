using Dawn.Core.Features.Util;
using System.Threading.Tasks;
using System.Windows;

namespace Dawn.Wpf.Util
{
    internal sealed class ClipboardService : IClipboardService
    {
        public Task SetData(string text)
        {
            Clipboard.SetData(DataFormats.Text, text);

            return Task.CompletedTask;
        }
    }
}
