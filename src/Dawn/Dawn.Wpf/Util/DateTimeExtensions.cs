using System;

namespace Dawn.Wpf
{
    public static class DateTimeExtensions
    {
        public static string FormatAsBackup(this DateTime dateTime)
        {
            return $"{dateTime:ddMMyyyyhhmmss}";
        }
    }
}
