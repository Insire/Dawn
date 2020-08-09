using FluentValidation;

namespace Dawn.Wpf
{
    public sealed class ValidationConfigurationViewModel : ValidationViewModel<ConfigurationViewModel>
    {
        public string TargetFolder
        {
            get
            {
                return ViewModel.TargetFolder;
            }
            set
            {
                ViewModel.TargetFolder = value;
                _ = Run().ContinueWith(t => OnPropertyChanged(nameof(TargetFolder)));
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

        public string FilePattern
        {
            get
            {
                return ViewModel.FilePattern;
            }
            set
            {
                ViewModel.FilePattern = value;
                _ = Run().ContinueWith(t => OnPropertyChanged(nameof(FilePattern)));
            }
        }

        public ValidationConfigurationViewModel(IValidator<ConfigurationViewModel> validator, ConfigurationViewModel viewModel)
            : base(validator, viewModel)
        {
        }
    }
}
