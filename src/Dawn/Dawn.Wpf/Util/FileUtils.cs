using Serilog;
using System;
using System.IO;
using System.IO.Compression;

namespace Dawn.Wpf
{
    public static class FileUtils
    {
        public static bool DeleteFor<T>(string file, ILogger log)
        {
            try
            {
                File.Delete(file);
                return true;
            }
            catch (Exception ex)
            {
                log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }

            return false;
        }

        public static bool MoveFor<T>(string from, string to, ILogger log, bool overwrite = false)
        {
            try
            {
                if (from != to)
                {
                    File.Move(from, to, overwrite);
                }

                return true;
            }
            catch (Exception ex)
            {
                log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }

            return false;
        }

        public static bool CopyFor<T>(string from, string to, ILogger log, bool overwrite = false)
        {
            try
            {
                if (from != to)
                {
                    File.Copy(from, to, overwrite);
                }
                return true;
            }
            catch (Exception ex)
            {
                log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }

            return false;
        }

        public static bool CopyFor<T>(string from, string to, ILogger log, DateTime timeStamp, bool overwrite = false, bool setLastWriteTime = false)
        {
            try
            {
                if (CopyFor<T>(from, to, log, overwrite))
                {
                    if (setLastWriteTime)
                    {
                        log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Debug, "Setting timestamp on {file} to {timeStamp}", to, timeStamp);
                        File.SetLastWriteTime(to, timeStamp);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Error, ex.ToString());
            }

            return false;
        }

        public static bool ExtractFor<T>(string from, string to, ILogger log, DateTime timeStamp, IProgress<double> progress, bool overwrite = false, bool setLastWriteTime = false)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(from))
                {
                    var count = 0d;
                    foreach (var entry in archive.Entries)
                    {
                        progress.Report(count * 100d / (archive.Entries.Count - 1));
                        // Gets the full path to ensure that relative segments are removed.
                        var destinationPath = Path.GetFullPath(Path.Combine(to, entry.FullName));

                        // Ordinal match is safest, case-sensitive volumes can be mounted within volumes that
                        // are case-insensitive.
                        if (destinationPath.StartsWith(to, StringComparison.Ordinal))
                        {
                            // IsDirectory ?
                            if (Path.GetFileName(destinationPath).Length == 0)
                            {
                                log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Debug, "Creating destination directory {directory}", destinationPath);
                                Directory.CreateDirectory(destinationPath);
                            }
                            else // is file
                            {
                                log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Debug, "Extracting file {file}", destinationPath);
                                entry.ExtractToFile(destinationPath, overwrite);

                                if (setLastWriteTime)
                                {
                                    log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Debug, "Setting timestamp on {file} to {timeStamp}", destinationPath, timeStamp);
                                    File.SetLastWriteTime(destinationPath, timeStamp);
                                }
                            }
                        }

                        count++;
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
