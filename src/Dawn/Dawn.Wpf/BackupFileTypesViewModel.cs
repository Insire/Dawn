using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;

namespace Dawn.Wpf
{
    public sealed class BackupFileTypesViewModel : ViewModelListBase<BackupFileTypeViewModel>
    {
        public BackupFileTypesViewModel(in IScarletCommandBuilder commandBuilder)
            : base(commandBuilder)
        {
            AddUnchecked(new BackupFileTypeViewModel("DLL", ".dll"));
            AddUnchecked(new BackupFileTypeViewModel("LST", ".lst"));
            AddUnchecked(new BackupFileTypeViewModel("EXE", ".exe"));
        }
    }
}
