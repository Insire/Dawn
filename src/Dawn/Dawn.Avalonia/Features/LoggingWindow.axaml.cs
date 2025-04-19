using CommunityToolkit.Mvvm.Input;
using Dawn.Core.Features.Logging;
using SukiUI.Controls;
using System;
using System.Windows.Input;

namespace Dawn.Avalonia
{
    public partial class LoggingWindow : SukiWindow
    {
        public ICommand CloseCommand { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public LoggingWindow()
        {
            InitializeComponent();
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

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
