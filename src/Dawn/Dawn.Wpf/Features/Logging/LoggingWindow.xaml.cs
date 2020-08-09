namespace Dawn.Wpf
{
    public partial class LoggingWindow
    {
        public LoggingWindow(LogViewModel logViewModel)
        {
            DataContext = logViewModel ?? throw new System.ArgumentNullException(nameof(logViewModel));

            InitializeComponent();
        }
    }
}
