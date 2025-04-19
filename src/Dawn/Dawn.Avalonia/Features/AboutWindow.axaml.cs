using Avalonia.Controls;
using Dawn.Core.Features.About;
using SukiUI.Controls;

namespace Dawn.Avalonia.Features
{
    public partial class AboutWindow : SukiWindow
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public AboutWindow()
        {
            InitializeComponent();
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public AboutWindow(AboutViewModel aboutViewModel)
        {
            DataContext = aboutViewModel ?? throw new System.ArgumentNullException(nameof(aboutViewModel));

            InitializeComponent();
        }
    }
}
