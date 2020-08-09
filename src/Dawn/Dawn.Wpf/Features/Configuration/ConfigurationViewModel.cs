using FluentValidation;
using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using System;

namespace Dawn.Wpf
{
    public sealed class ConfigurationViewModel : ViewModelBase
    {
        private readonly ConfigurationModel _model;

        private string _targetFolder;
        public string TargetFolder
        {
            get { return _targetFolder; }
            set { SetValue(ref _targetFolder, value, onChanged: () => _model.TargetFolder = value); }
        }

        private string _backupFolder;
        public string BackupFolder
        {
            get { return _backupFolder; }
            set { SetValue(ref _backupFolder, value, onChanged: () => _model.BackupFolder = value); }
        }

        private string _filePattern;
        public string FilePattern
        {
            get { return _filePattern; }
            set { SetValue(ref _filePattern, value, onChanged: () => _model.FilePattern = value); }
        }

        public BackupFileTypesViewModel BackupFileTypes { get; }

        public ValidationConfigurationViewModel Validation { get; }

        public ConfigurationViewModel(IScarletCommandBuilder commandBuilder, ConfigurationModel model, IValidator<ConfigurationViewModel> validator)
            : base(commandBuilder)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            _targetFolder = _model.TargetFolder;
            _backupFolder = _model.BackupFolder;
            _filePattern = _model.FilePattern;

            BackupFileTypes = new BackupFileTypesViewModel(commandBuilder, model);

            Validation = new ValidationConfigurationViewModel(validator, this);
        }
    }
}
