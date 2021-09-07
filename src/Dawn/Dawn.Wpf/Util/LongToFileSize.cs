using MvvmScarletToolkit;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Dawn.Wpf
{
    [ValueConversion(typeof(long), typeof(string))]
    public sealed class LongToFileSize : ConverterMarkupExtension<LongToFileSize>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long number)
            {
                return number.GetBytesReadable();
            }

            return Binding.DoNothing;
        }
    }
}
