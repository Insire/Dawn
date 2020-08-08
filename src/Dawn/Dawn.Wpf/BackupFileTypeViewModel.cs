using MvvmScarletToolkit.Observables;

namespace Dawn.Wpf
{
    public sealed class BackupFileTypeViewModel : ObservableObject
    {
        private string _type;
        public string Type
        {
            get { return _type; }
            private set { SetValue(ref _type, value); }
        }

        private string _extension;
        public string Extension
        {
            get { return _extension; }
            private set { SetValue(ref _extension, value); }
        }

        public BackupFileTypeViewModel(string type, string extension)
        {
            Type = type ?? throw new System.ArgumentNullException(nameof(type));
            Extension = extension ?? throw new System.ArgumentNullException(nameof(extension));
        }
    }
}
