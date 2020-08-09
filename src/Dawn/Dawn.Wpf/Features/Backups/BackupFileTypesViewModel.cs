using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;

namespace Dawn.Wpf
{
    public sealed class BackupFileTypesViewModel : ViewModelListBase<BackupFileTypeViewModel>
    {
        public BackupFileTypesViewModel(in IScarletCommandBuilder commandBuilder, ConfigurationModel model)
            : base(commandBuilder)
        {
            if (model.BackupFileTypes != null)
            {
                foreach (var type in model.BackupFileTypes)
                {
                    AddUnchecked(new BackupFileTypeViewModel(type));
                }
            }
        }
    }
}
