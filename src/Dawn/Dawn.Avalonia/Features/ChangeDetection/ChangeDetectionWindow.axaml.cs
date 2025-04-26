using CommunityToolkit.Mvvm.Input;
using Dawn.Core.Features.ChangeDetection;
using SukiUI.Controls;
using System.Windows.Input;

namespace Dawn.Avalonia.Features.ChangeDetection
{
    public partial class ChangeDetectionWindow : SukiWindow
    {
        private readonly ChangeDetectionViewModel _changeDetectionViewModel;

        public ICommand CloseCommand { get; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public ChangeDetectionWindow()
        {
            InitializeComponent();
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public ChangeDetectionWindow(ChangeDetectionViewModel changeDetectionViewModel)
        {
            DataContext = _changeDetectionViewModel = changeDetectionViewModel;
            CloseCommand = new RelayCommand(CloseInternal, CanClose);

            InitializeComponent();
        }

        private void CloseInternal()
        {
            Close();
        }

        private bool CanClose()
        {
            return !_changeDetectionViewModel.IsBusy;
        }
    }
}
