using MvvmScarletToolkit;
using MvvmScarletToolkit.Commands;
using System;
using System.Windows.Input;

namespace Dawn.Wpf
{
    public partial class LoggingWindow
    {
        public ICommand CloseCommand { get; }

        public LoggingWindow(LogViewModel logViewModel)
        {
            DataContext = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));

            CloseCommand = new RelayCommand(ScarletCommandBuilder.Default, CloseInternal);

            InitializeComponent();
        }

        private void CloseInternal()
        {
            DialogResult = true;
        }
    }
}
