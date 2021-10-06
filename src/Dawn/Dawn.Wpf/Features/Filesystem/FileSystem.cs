using Ookii.Dialogs.Wpf;
using Serilog;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Dawn.Wpf
{
    public sealed class FileSystem : IFileSystem
    {
        public bool TrySelectFiles(out string[] files)
        {
            var dlg = new VistaOpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DereferenceLinks = true,
                AddExtension = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Multiselect = true,
                Title = "Select files",
                ValidateNames = true,
            };

            var result = dlg.ShowDialog();

            if (result == true)
            {
                files = dlg.FileNames;
            }
            else
            {
                files = null;
            }

            return result ?? false;
        }

        public bool TrySelectFolder(out string folder)
        {
            var dlg = new VistaFolderBrowserDialog
            {
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true,
                Description = "Select a folder"
            };

            var result = dlg.ShowDialog();

            if (result == true)
            {
                folder = dlg.SelectedPath;
            }
            else
            {
                folder = null;
            }

            return result ?? false;
        }

        public void WriteAllText(string path, string contents, Encoding encoding)
        {
            File.WriteAllText(path, contents, encoding);
        }

        public string ReadAllText(string path, Encoding encoding)
        {
            return File.ReadAllText(path, encoding);
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetDirectories(path, searchPattern, searchOption);
        }

        public bool DeleteFor<T>(string file, ILogger log)
        {
            try
            {
                DeleteFile(file);
                return true;
            }
            catch (Exception ex)
            {
                log.ForContext<T>().LogError(ex);
            }

            return false;
        }

        public bool MoveFor<T>(string from, string to, ILogger log, bool overwrite = false)
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
                log.ForContext<T>().LogError(ex);
            }

            return false;
        }

        public bool CopyFor<T>(string from, string to, ILogger log, bool overwrite = false)
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
                log.ForContext<T>().LogError(ex);
            }

            return false;
        }

        public bool CopyFor<T>(string from, string to, ILogger log, DateTime timeStamp, bool overwrite = false, bool setLastWriteTime = false)
        {
            try
            {
                if (CopyFor<T>(from, to, log, overwrite))
                {
                    if (setLastWriteTime)
                    {
                        log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Debug, "Setting timestamp on {File} to {TimeStamp}", to, timeStamp);
                        File.SetLastWriteTime(to, timeStamp);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                log.ForContext<T>().LogError(ex);
            }

            return false;
        }

        public bool ExtractFor<T>(string from, string to, ILogger log, DateTime timeStamp, IProgress<decimal> progress, bool overwrite = false, bool setLastWriteTime = false)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(from))
                {
                    var count = 0;
                    foreach (var entry in archive.Entries)
                    {
                        progress?.Report(count, archive.Entries.Count);
                        // Gets the full path to ensure that relative segments are removed.
                        var destinationPath = Path.GetFullPath(Path.Combine(to, entry.FullName));

                        // Ordinal match is safest, case-sensitive volumes can be mounted within volumes that
                        // are case-insensitive.
                        if (destinationPath.StartsWith(to, StringComparison.Ordinal))
                        {
                            // IsDirectory ?
                            var fileName = Path.GetFileName(destinationPath);
                            if (fileName.Length == 0)
                            {
                                log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Debug, "Creating destination directory {FolderPath}", destinationPath);
                                CreateDirectory(destinationPath);
                            }
                            else // is file
                            {
                                var directoryName = destinationPath.Replace(fileName, "");
                                CreateDirectory(directoryName);

                                log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Debug, "Extracting file {File}", destinationPath);
                                entry.ExtractToFile(destinationPath, overwrite);

                                if (setLastWriteTime)
                                {
                                    log.ForContext<T>().Write(Serilog.Events.LogEventLevel.Debug, "Setting timestamp on {File} to {TimeStamp}", destinationPath, timeStamp);
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
                log.ForContext<T>().LogError(ex);
            }

            return false;
        }
    }
}
