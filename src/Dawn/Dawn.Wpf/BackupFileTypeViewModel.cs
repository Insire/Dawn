using MvvmScarletToolkit.Observables;
using System;

namespace Dawn.Wpf
{
    public sealed class BackupFileTypeViewModel : ObservableObject
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            private set { SetValue(ref _name, value); }
        }

        private string _extension;
        public string Extension
        {
            get { return _extension; }
            private set { SetValue(ref _extension, value); }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetValue(ref _isEnabled, value); }
        }

        public BackupFileTypeViewModel(BackupFileTypeModel model)
            : this(model?.Name, model?.Extension, model?.IsEnabled ?? false)
        {
        }

        public BackupFileTypeViewModel(string type, string extension, bool isEnabled)
        {
            Name = type ?? throw new ArgumentNullException(nameof(type));
            Extension = extension ?? throw new ArgumentNullException(nameof(extension));
            IsEnabled = isEnabled;
        }
    }
}
