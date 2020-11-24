using Serilog;
using System;
using System.IO;
using System.IO.Compression;

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

        public static bool CopyFor<T>(string from, string to, ILogger log, DateTime timeStamp, bool overwrite = false)
        {
            try
            {
                if (CopyFor<T>(from, to, log, overwrite))
                {
                    File.SetLastWriteTime(to, timeStamp);
                    return true;
                }
            }
            catch (Exception ex)
            {
                log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }

            return false;
        }

        public static bool ExtractFor<T>(string from, string to, ILogger log, DateTime timeStamp, bool overwrite = false)
        {
            try
            {
                //ZipFile.ExtractToDirectory(from, to, overwrite);
                using (var archive = ZipFile.OpenRead(from))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // Gets the full path to ensure that relative segments are removed.
                        var destinationPath = Path.GetFullPath(Path.Combine(to, entry.FullName));

                        // Ordinal match is safest, case-sensitive volumes can be mounted within volumes that
                        // are case-insensitive.
                        if (destinationPath.StartsWith(to, StringComparison.Ordinal))
                        {
                            // IsDirectory ?
                            if (Path.GetFileName(destinationPath).Length == 0)
                            {
                                Directory.CreateDirectory(destinationPath);
                            }
                            else // is file
                            {
                                entry.ExtractToFile(destinationPath, overwrite);

                                File.SetLastWriteTime(destinationPath, timeStamp);
                            }
                        }
                    }
                }

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
