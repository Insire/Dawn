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

        private bool _isKeepinForeground;
        [Display(Name = "Keep in foreground", Description = "Keeps the window in the foreground by setting Topmost")]
        [Required]
        public bool IsKeepinForeground
        {
            get { return _isKeepinForeground; }
            set
            {
                if (SetProperty(ref _isKeepinForeground, value, true))
                {
                    Model.IsKeepinForeground = value;
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
            _isKeepinForeground = Model.IsKeepinForeground;

            IsLocalConfig = Model.IsLocalConfig;

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
