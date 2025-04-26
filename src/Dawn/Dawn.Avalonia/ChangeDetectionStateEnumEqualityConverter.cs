using Avalonia.Data;
using Avalonia.Data.Converters;
using Dawn.Core.Features.ChangeDetection;
using System;
using System.Globalization;

namespace Dawn.Avalonia
{
    public sealed class ChangeDetectionStateEnumEqualityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not ChangeDetectionState state)
            {
                return false;
            }

            if (parameter is ChangeDetectionState param)
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
