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

        private bool _isValid;
        public bool IsValid
        {
            get { return _isValid; }
            private set { SetValue(ref _isValid, value); }
        }

        public BackupFileTypesViewModel BackupFileTypes { get; }

        public ConfigurationViewModel(IScarletCommandBuilder commandBuilder, BackupFileTypesViewModel backupFileTypes, ConfigurationModel model)
            : base(commandBuilder)
        {
            BackupFileTypes = backupFileTypes ?? throw new ArgumentNullException(nameof(backupFileTypes));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            _targetFolder = _model.TargetFolder;
            _backupFolder = _model.BackupFolder;
            _filePattern = _model.FilePattern;
        }
    }
}
