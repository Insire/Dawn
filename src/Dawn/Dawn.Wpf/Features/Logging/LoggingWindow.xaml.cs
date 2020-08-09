namespace Dawn.Wpf
{
    public partial class LoggingWindow
    {
        private readonly LogViewModel _logViewModel;

        public LoggingWindow(LogViewModel logViewModel)
        {
            DataContext = _logViewModel = logViewModel ?? throw new System.ArgumentNullException(nameof(logViewModel));

            InitializeComponent();
        }
    }
}
