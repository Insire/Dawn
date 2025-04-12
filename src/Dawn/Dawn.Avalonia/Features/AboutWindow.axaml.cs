using Avalonia.Controls;
using Dawn.Core.Features.About;

namespace Dawn.Avalonia.Features
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        public AboutWindow(AboutViewModel aboutViewModel)
        {
            DataContext = aboutViewModel ?? throw new System.ArgumentNullException(nameof(aboutViewModel));

            InitializeComponent();
        }
    }
}
