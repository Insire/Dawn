using Avalonia.Data;
using Avalonia.Data.Converters;
using Dawn.Core.Features.Util;
using System;
using System.Globalization;

namespace Dawn.Avalonia
{
    public sealed class LongToFileSizeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long number)
            {
                return number.GetBytesReadable();
            }

            return BindingOperations.DoNothing;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // If we want to convert back, we need to subtract instead of add.
            return BindingOperations.DoNothing;
        }
    }
}
