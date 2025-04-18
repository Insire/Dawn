using MvvmScarletToolkit;
using Serilog.Events;
using System;
using System.Globalization;
using System.Threading;

namespace Dawn.Wpf
{
    public sealed class LogEventRender : ConverterMarkupExtension<LogEventRender>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogEvent @event)
            {
                return @event.RenderMessage(Thread.CurrentThread.CurrentUICulture);
            }

            return string.Empty;
        }
    }
}
