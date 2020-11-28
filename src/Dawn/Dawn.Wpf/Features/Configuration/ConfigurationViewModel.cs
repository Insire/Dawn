using FluentValidation;
using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using System;

namespace Dawn.Wpf
{
    public sealed class ConfigurationViewModel : ViewModelBase
    {
        private readonly ConfigurationModel _model;

        private string _deploymentFolder;
        public string DeploymentFolder
        {
            get { return _deploymentFolder; }
            set { SetValue(ref _deploymentFolder, value, onChanged: () => Model.DeploymentFolder = value); }
        }

        private string _backupFolder;
        public string BackupFolder
        {
            get { return _backupFolder; }
            set { SetValue(ref _backupFolder, value, onChanged: () => Model.BackupFolder = value); }
        }

        public BackupFileTypesViewModel BackupFileTypes { get; }

        public ValidationConfigurationViewModel Validation { get; }

        public ConfigurationModel Model => _model;

        /// <summary>
        /// configuration was loaded from a local file
        /// </summary>
        public bool IsLocalConfig { get; }

        public ConfigurationViewModel(IScarletCommandBuilder commandBuilder, ConfigurationModel model, IValidator<ConfigurationViewModel> validator)
            : base(commandBuilder)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            _deploymentFolder = _model.DeploymentFolder;
            _backupFolder = _model.BackupFolder;

            IsLocalConfig = _model.IsLocalConfig;

            BackupFileTypes = new BackupFileTypesViewModel(commandBuilder, model);

            Validation = new ValidationConfigurationViewModel(commandBuilder, validator, this);
        }
    }
}
