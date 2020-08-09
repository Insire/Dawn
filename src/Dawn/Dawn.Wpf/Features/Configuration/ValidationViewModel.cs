using FluentValidation;
using FluentValidation.Results;
using MvvmScarletToolkit.Observables;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Dawn.Wpf
{
    public abstract class ValidationViewModel<T> : ObservableObject, INotifyDataErrorInfo
        where T : class, INotifyPropertyChanged
    {
        private readonly IValidator<T> _validator;
        private ValidationResult _result;

        public T ViewModel { get; }

        private bool _hasErrors;
        public bool HasErrors
        {
            get { return _hasErrors; }
            private set { SetValue(ref _hasErrors, value); }
        }

        private bool _isValid;
        public bool IsValid
        {
            get { return _isValid; }
            private set { SetValue(ref _isValid, value); }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        protected ValidationViewModel(IValidator<T> validator, T viewModel)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            IsValid = true;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return _result?.Errors.Where(p => p.PropertyName == propertyName).Select(p => p.ErrorMessage) ?? Enumerable.Empty<string>();
        }

        public async Task Run()
        {
            _result = await _validator.ValidateAsync<T>(ViewModel);

            HasErrors = !_result.IsValid;
            IsValid = _result.IsValid;

            for (var i = 0; i < _result.Errors.Count; i++)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(_result.Errors[i].PropertyName));
            }
        }
    }
}
