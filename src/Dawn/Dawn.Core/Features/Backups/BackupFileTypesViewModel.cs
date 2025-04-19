namespace Dawn.Core.Features.Backups
{
    public sealed class BackupFileTypesViewModel : ViewModelListBase<BackupFileTypeViewModel>
    {
        public BackupFileTypesViewModel(in IScarletCommandBuilder commandBuilder, ConfigurationModel model)
            : base(commandBuilder)
        {
            foreach (var type in model.BackupFileTypes)
            {
                AddUnchecked(new BackupFileTypeViewModel(type));
            }
        }
    }
}
