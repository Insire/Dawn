using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public partial class LoggingWindow
    {
        public bool ShowErrors
        {
            get { return (bool)GetValue(ShowErrorsProperty); }
            set { SetValue(ShowErrorsProperty, value); }
        }

        public static readonly DependencyProperty ShowErrorsProperty = DependencyProperty.Register(
            nameof(ShowErrors),
            typeof(bool),
            typeof(LoggingWindow),
            new PropertyMetadata(false));

        public ICommand CloseCommand { get; }

        public ICommand ToggleViewCommand { get; }

        public LoggingWindow(LogViewModel logViewModel)
        {
            DataContext = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));

            CloseCommand = new RelayCommand(CloseImpl);
            ToggleViewCommand = new RelayCommand(ToggleViewImpl);

            InitializeComponent();
        }

        private void CloseImpl()
        {
            DialogResult = true;
        }

        private void ToggleViewImpl()
        {
            ShowErrors = !ShowErrors;
        }
    }
}
