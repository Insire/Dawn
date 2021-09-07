using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public partial class ChangeDetectionWindow
    {
        public ICommand CloseCommand { get; }

        public ChangeDetectionWindow(ChangeDetectionViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            CloseCommand = new RelayCommand(CloseInternal);

            InitializeComponent();
        }

        private void CloseInternal()
        {
            DialogResult = true;
        }
    }
}
