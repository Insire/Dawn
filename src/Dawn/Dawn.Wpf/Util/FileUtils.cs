using Serilog;
using System;
using System.IO;

namespace Dawn.Wpf
{
    public static class FileUtils
    {
        public static bool CopyFor<T>(string from, string to, ILogger log, bool overwrite = false)
        {
            try
            {
                File.Copy(from, to, overwrite);
                return true;
            }
            catch (Exception ex)
            {
                log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }

            return false;
        }
    }
}
