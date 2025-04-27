using SukiUI.Controls;
using SukiUI.Dialogs;
using System;

namespace Dawn.Avalonia
{
    public sealed class DialogHostFocus : IDisposable
    {
        private readonly SukiDialogHost _host;
        private readonly ISukiDialogManager _manager;

        public DialogHostFocus(SukiDialogHost host, ISukiDialogManager manager)
        {
            _host = host;
            _manager = manager;
            host.Manager = null;
        }

        public void Dispose()
        {
            _host.Manager = _manager;
        }
    }
}
