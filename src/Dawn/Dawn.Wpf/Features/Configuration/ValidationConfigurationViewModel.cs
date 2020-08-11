using FluentValidation;
using MvvmScarletToolkit;
using System.Threading;

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
                _ = Validate(CancellationToken.None).ContinueWith(t => OnPropertyChanged(nameof(DeploymentFolder)));
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
                _ = Validate(CancellationToken.None).ContinueWith(t => OnPropertyChanged(nameof(BackupFolder)));
            }
        }

        public ValidationConfigurationViewModel(in IScarletCommandBuilder commandBuilder, IValidator<ConfigurationViewModel> validator, ConfigurationViewModel viewModel)
            : base(commandBuilder, validator, viewModel)
        {
        }
    }
}