using Dawn.Core.Features.Util;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace Dawn.Wpf.Util
{
    internal sealed class ClipboardService : IClipboardService
    {
        public Task SetDataAsync(string text)
        {
            try
            {
                Clipboard.SetText($"json='{text}'", TextDataFormat.Text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return Task.CompletedTask;
        }

        public Task SetTextAsync(string text)
        {
            try
            {
                Clipboard.SetDataObject($"json='{text}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return Task.CompletedTask;
        }
    }
}
