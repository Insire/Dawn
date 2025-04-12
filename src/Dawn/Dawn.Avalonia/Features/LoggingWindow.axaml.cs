using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Dawn.Core.Features.Logging;
using System.Windows.Input;
using System;

namespace Dawn.Avalonia
{
    public partial class LoggingWindow : Window
    {
        public ICommand CloseCommand { get; }

        public LoggingWindow()
        {
            InitializeComponent();
        }

        public LoggingWindow(LogViewModel logViewModel)
        {
            DataContext = logViewModel ?? throw new ArgumentNullException(nameof(logViewModel));

            CloseCommand = new RelayCommand(CloseImpl);

            InitializeComponent();
        }

        private void CloseImpl()
        {
            Close();
        }
    }
}
