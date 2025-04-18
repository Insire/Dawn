using System.Diagnostics;

namespace Dawn.Core.Features.Backups
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class BackupFileTypeViewModel : ObservableObject
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            private set { SetProperty(ref _name, value); }
        }

        private string _extension;
        public string Extension
        {
            get { return _extension; }
            private set { SetProperty(ref _extension, value); }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProperty(ref _isEnabled, value); }
        }

        public BackupFileTypeViewModel(BackupFileTypeModel model)
            : this(model.Name, model.Extension, model?.IsEnabled ?? false)
        {
        }

        public BackupFileTypeViewModel(string? name, string? extension, bool isEnabled)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _extension = extension ?? throw new ArgumentNullException(nameof(extension));
            _isEnabled = isEnabled;
        }

        private string GetDebuggerDisplay()
        {
            return $"{Name} - {Extension}";
        }
    }
}
