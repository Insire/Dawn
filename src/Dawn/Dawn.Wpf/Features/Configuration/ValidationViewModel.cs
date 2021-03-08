using FluentValidation;
using FluentValidation.Results;
using MvvmScarletToolkit;
using MvvmScarletToolkit.Observables;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public abstract class ValidationViewModel<T> : ViewModelBase, INotifyDataErrorInfo
        where T : class, INotifyPropertyChanged
    {
        private readonly IValidator<T> _validator;
        private ValidationResult _result;

        protected T ViewModel { get; }

        private bool _hasErrors;
        public bool HasErrors
        {
            get { return _hasErrors; }
            private set { SetProperty(ref _hasErrors, value); }
        }

        private bool _isValid;
        public bool IsValid
        {
            get { return _isValid; }
            private set { SetProperty(ref _isValid, value); }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public ICommand ValidateCommand { get; }

        protected ValidationViewModel(in IScarletCommandBuilder commandBuilder, IValidator<T> validator, T viewModel)
            : base(commandBuilder)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            ValidateCommand = commandBuilder
                .Create(Validate, CanValidate)
                .WithAsyncCancellation()
                .WithSingleExecution()
                .Build();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return _result?.Errors.Where(p => p.PropertyName == propertyName).Select(p => p.ErrorMessage) ?? Enumerable.Empty<string>();
        }

        protected async Task Validate(CancellationToken token)
        {
            _result = await _validator.ValidateAsync(ViewModel, options => options.IncludeAllRuleSets(), token);

            HasErrors = !_result.IsValid;
            IsValid = _result.IsValid;

            for (var i = 0; i < _result.Errors.Count; i++)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(_result.Errors[i].PropertyName));
            }
        }

        private bool CanValidate()
        {
            return !IsBusy;
        }
    }
}
