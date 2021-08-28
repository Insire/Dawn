using Microsoft.Toolkit.Mvvm.ComponentModel;
using MvvmScarletToolkit;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public sealed class ConfigurationViewModel : ObservableValidator
    {
        private string _deploymentFolder;
        [Display(Name = "Deployment Folder", Description = "The folder where files will get deployed to")]
        [Required]
        [MinLength(2)]
        public string DeploymentFolder
        {
            get { return _deploymentFolder; }
            set
            {
                if (SetProperty(ref _deploymentFolder, value, true))
                {
                    Model.DeploymentFolder = value;
                }
            }
        }

        private string _backupFolder;

        [Display(Name = "Backup Folder", Description = "The folder where Dawn will store backups of the files that are going to be deployed")]
        [Required]
        [MinLength(2)]
        public string BackupFolder
        {
            get { return _backupFolder; }
            set
            {
                if (SetProperty(ref _backupFolder, value, true))
                {
                    Model.BackupFolder = value;
                }
            }
        }

        private bool _updateTimeStampOnRestore;

        [Display(Name = "Update timestamp on restore", Description = "Whether to change the last write time for a file when restoring files")]
        public bool UpdateTimeStampOnRestore
        {
            get { return _updateTimeStampOnRestore; }
            set
            {
                if (SetProperty(ref _updateTimeStampOnRestore, value, true))
                {
                    Model.UpdateTimeStampOnRestore = value;
                }
            }
        }

        private bool _updateTimeStampOnApply;
        [Display(Name = "Update timestamp on applying an update", Description = "Whether to change the last write time for a file when adding files")]
        public bool UpdateTimeStampOnApply
        {
            get { return _updateTimeStampOnApply; }
            set
            {
                if (SetProperty(ref _updateTimeStampOnApply, value, true))
                {
                    Model.UpdateTimeStampOnApply = value;
                }
            }
        }

        public ICommand ValidateCommand { get; }

        public BackupFileTypesViewModel BackupFileTypes { get; }

        public ConfigurationModel Model { get; }

        /// <summary>
        /// configuration was loaded from a local file
        /// </summary>
        public bool IsLocalConfig { get; }

        public ConfigurationViewModel(IScarletCommandBuilder commandBuilder, ConfigurationModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            _deploymentFolder = Model.DeploymentFolder;
            _backupFolder = Model.BackupFolder;
            _updateTimeStampOnApply = Model.UpdateTimeStampOnApply ?? false;
            _updateTimeStampOnRestore = model.UpdateTimeStampOnRestore ?? false;

            IsLocalConfig = Model.IsLocalConfig ?? true;

            BackupFileTypes = new BackupFileTypesViewModel(commandBuilder, model);

            ValidateCommand = commandBuilder
                      .Create(Validate)
                      .WithAsyncCancellation()
                      .WithSingleExecution()
                      .Build();
        }

        public Task Validate()
        {
            ValidateAllProperties();

            return Task.CompletedTask;
        }
    }
}
