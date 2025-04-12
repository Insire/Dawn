using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Dawn.Core.Features.Util;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dawn.Avalonia.Infrastructure
{
    public sealed class ClipboardService : IClipboardService
    {
        private readonly IClassicDesktopStyleApplicationLifetime _lifetime;

        public ClipboardService(IClassicDesktopStyleApplicationLifetime lifetime)
        {
            _lifetime = lifetime;
        }

        public async Task SetTextAsync(string text)
        {
            var clipboard = _lifetime.MainWindow?.Clipboard;
            if (clipboard is null)
            {
                return;
            }

            try
            {
                await clipboard.SetTextAsync(text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public async Task SetDataAsync(string text)
        {
            var clipboard = _lifetime.MainWindow?.Clipboard;
            if (clipboard is null)
            {
                return;
            }

            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Text, text);

            try
            {
                await clipboard.SetDataObjectAsync(dataObject);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
