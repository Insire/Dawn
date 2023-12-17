namespace Dawn.Wpf
{
    public sealed partial class AboutWindow
    {
        public AboutWindow(AboutViewModel aboutViewModel)
        {
            DataContext = aboutViewModel ?? throw new System.ArgumentNullException(nameof(aboutViewModel));

            InitializeComponent();
        }
    }
}
