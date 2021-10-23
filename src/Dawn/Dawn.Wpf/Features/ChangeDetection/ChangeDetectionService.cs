using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Dawn.Wpf
{
    public sealed class ChangeDetectionService
    {
        private readonly ILogger _log;
        private readonly ConfigurationViewModel _configuration;

        public ChangeDetectionService(ILogger log, ConfigurationViewModel configuration)
        {
            _log = log.ForContext<ChangeDetectionService>() ?? throw new ArgumentNullException(nameof(log));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Task<List<FilePairViewModel>> DetectChanges(BackupViewModel backup)
        {
            return Task.Run(() =>
            {
                using (var mySHA256 = SHA256.Create())
                {
                    var results = new List<FilePairViewModel>();
                    foreach (var entry in backup.Items)
                    {
                        if (entry.IsFile && entry is FileInfoViewModel fileInfo)
                        {
                            var fileName = Path.GetFileName(fileInfo.FullPath);
                            var deploymentFileName = Path.Combine(_configuration.DeploymentFolder, fileName);
                            var destination = new FileInfoViewModel(deploymentFileName);
                            var pair = new FilePairViewModel(fileInfo, destination);

                            var info = new FileInfo(pair.Source.FullPath);
                            pair.Source.Attributes = info.Attributes;
                            pair.Source.IsReadOnly = info.IsReadOnly;
                            pair.Source.CreationTime = info.CreationTime;
                            pair.Source.LastAccessTime = info.LastAccessTime;
                            pair.Source.LastWriteTime = info.LastWriteTime;
                            pair.Source.Exists = info.Exists;

                            if (info.Exists)
                            {
                                pair.Source.Length = info.Length;

                                using (var fs = new FileStream(pair.Source.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    pair.Source.Hash = ComputeHash(mySHA256, fs);

                                    pair.Source.IsNetAssembly = IsAssembly(fs);
                                }
                            }

                            info = new FileInfo(pair.Destination.FullPath);
                            pair.Destination.Attributes = info.Attributes;
                            pair.Destination.IsReadOnly = info.IsReadOnly;
                            pair.Destination.CreationTime = info.CreationTime;
                            pair.Destination.LastAccessTime = info.LastAccessTime;
                            pair.Destination.LastWriteTime = info.LastWriteTime;
                            pair.Destination.Exists = info.Exists;

                            if (info.Exists)
                            {
                                pair.Destination.Length = info.Length;

                                using (var fs = new FileStream(pair.Destination.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    pair.Destination.Hash = ComputeHash(mySHA256, fs);

                                    pair.Destination.IsNetAssembly = IsAssembly(fs);
                                }
                            }

                            pair.UpdateChangeState();
                            results.Add(pair);
                        }
                    }

                    return results;
                }
            });
        }

        private bool IsAssembly(FileStream fs)
        {
            try
            {
                fs.Position = 0;
                // Try to read CLI metadata from the PE file.
                using (var peReader = new PEReader(fs))
                {
                    if (!peReader.HasMetadata)
                    {
                        return false; // File does not have CLI metadata.
                    }

                    // Check that file has an assembly manifest.
                    var reader = peReader.GetMetadataReader();

                    return reader.IsAssembly;
                }
            }
            catch (BadImageFormatException ex)
            {
                _log.LogError(ex);
                return false;
            }
            catch (FileNotFoundException ex)
            {
                _log.LogError(ex);
            }

            return false;
        }

        private string ComputeHash(SHA256 sHA256, FileStream fs)
        {
            try
            {
                fs.Position = 0;

                var hashValue = sHA256.ComputeHash(fs);

                return PrintByteArray(hashValue);
            }
            catch (IOException ex)
            {
                _log.LogError(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _log.LogError(ex);
            }

            return null;
        }

        public static string PrintByteArray(byte[] array)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < array.Length; i++)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0:X2}", array[i]);
                if ((i % 4) == 3)
                {
                    builder.Append(' ');
                }
            }

            return builder.ToString();
        }
    }
}
