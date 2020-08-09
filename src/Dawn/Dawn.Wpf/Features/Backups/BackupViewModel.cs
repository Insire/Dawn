using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;

namespace Dawn.Wpf
{
    /// <summary>
    /// a file that belongs to an update
    /// </summary>
    public sealed class BackupViewModel : ViewModelListBase<ViewModelContainer<string>>
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            private set { SetValue(ref _name, value); }
        }

        public BackupViewModel(in IScarletCommandBuilder commandBuilder, string name)
            : base(commandBuilder)
        {
            Name = name ?? throw new System.ArgumentNullException(nameof(name));
        }
    }
}
