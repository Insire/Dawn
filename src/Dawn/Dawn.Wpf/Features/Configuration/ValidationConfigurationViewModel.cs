using FluentValidation;

namespace Dawn.Wpf
{
    public sealed class ValidationConfigurationViewModel : ValidationViewModel<ConfigurationViewModel>
    {
        public string DeploymentFolder
        {
            get
            {
                return ViewModel.DeploymentFolder;
            }
            set
            {
                ViewModel.DeploymentFolder = value;
                _ = Run().ContinueWith(t => OnPropertyChanged(nameof(DeploymentFolder)));
            }
        }

        public string BackupFolder
        {
            get
            {
                return ViewModel.BackupFolder;
            }
            set
            {
                ViewModel.BackupFolder = value;
                _ = Run().ContinueWith(t => OnPropertyChanged(nameof(BackupFolder)));
            }
        }

        public ValidationConfigurationViewModel(IValidator<ConfigurationViewModel> validator, ConfigurationViewModel viewModel)
            : base(validator, viewModel)
        {
        }
    }
}
