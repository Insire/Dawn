using Dawn.Core.Features.ChangeDetection;
using SukiUI.Controls;

namespace Dawn.Avalonia.Features
{
    public partial class ChangeDetectionWindow : SukiWindow
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public ChangeDetectionWindow()
        {
            InitializeComponent();
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public ChangeDetectionWindow(ChangeDetectionViewModel changeDetectionViewModel)
        {
            DataContext = changeDetectionViewModel ?? throw new System.ArgumentNullException(nameof(changeDetectionViewModel));

            InitializeComponent();
        }
    }
}
