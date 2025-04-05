namespace Dawn.Core.Features.Util
{
    public static class DateTimeExtensions
    {
        public static string FormatAsBackup(this DateTime dateTime)
        {
            return $"{dateTime:ddMMyyyyHHmmss}";
        }
    }
}
