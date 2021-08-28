using System;

namespace Dawn.Wpf
{
    public static class ProgressExtensions
    {
        public static void Report(this IProgress<decimal> progress, int current, int total)
        {
            if (total == 0)
            {
                progress.Report(100m);
                return;
            }

            progress.Report(current * 100m / total);
        }
    }
}
