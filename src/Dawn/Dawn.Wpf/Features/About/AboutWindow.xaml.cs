namespace Dawn.Wpf
{
    public sealed partial class AboutWindow
    {
        private readonly AboutViewModel _aboutViewModel;

        public AboutWindow(AboutViewModel aboutViewModel)
        {
            DataContext = _aboutViewModel = aboutViewModel ?? throw new System.ArgumentNullException(nameof(aboutViewModel));

            InitializeComponent();
        }
    }
}
