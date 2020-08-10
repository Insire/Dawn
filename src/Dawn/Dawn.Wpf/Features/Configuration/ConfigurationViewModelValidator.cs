using FluentValidation;

namespace Dawn.Wpf
{
    internal sealed class ConfigurationViewModelValidator : AbstractValidator<ConfigurationViewModel>
    {
        public ConfigurationViewModelValidator()
        {
            RuleFor(p => p.BackupFolder).NotEmpty();
            RuleFor(p => p.DeploymentFolder).NotEmpty();
        }
    }
}
