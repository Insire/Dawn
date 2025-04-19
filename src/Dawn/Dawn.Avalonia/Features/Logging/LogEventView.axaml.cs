using Avalonia.Controls;
using Avalonia.Interactivity;
using Dawn.Core.Features.Logging;

namespace Dawn.Avalonia.Features.Logging
{
    public partial class LogEventView : UserControl
    {
        public LogEventView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is LogEventViewModel vm)
            {
                vm.RenderCommand.Execute(null);
            }
        }
    }
}
