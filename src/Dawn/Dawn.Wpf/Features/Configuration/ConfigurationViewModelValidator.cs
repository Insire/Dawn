using FluentValidation;

namespace Dawn.Wpf
{
    internal sealed class ConfigurationViewModelValidator : AbstractValidator<ConfigurationViewModel>
    {
        public ConfigurationViewModelValidator()
        {
            RuleSet("Default", () =>
            {
                RuleFor(p => p.BackupFolder).NotEmpty();
                RuleFor(p => p.DeploymentFolder).NotEmpty();
            });
        }
    }
}