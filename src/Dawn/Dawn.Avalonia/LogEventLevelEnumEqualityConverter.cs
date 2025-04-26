using Avalonia.Data;
using Avalonia.Data.Converters;
using Serilog.Events;
using System;
using System.Globalization;

namespace Dawn.Avalonia
{
    public sealed class LogEventLevelEnumEqualityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not LogEventLevel state)
            {
                return false;
            }

            if (parameter is LogEventLevel param)
            {
                return state == param;
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // If we want to convert back, we need to subtract instead of add.
            return BindingOperations.DoNothing;
        }
    }
}
