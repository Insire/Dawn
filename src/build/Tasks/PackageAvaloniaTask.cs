using Cake.Common.IO;
using Cake.Compression;
using Cake.Core.IO;
using Cake.Frosting;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Build
{
    [TaskName("PackageAvalonia")]
    [IsDependentOn(typeof(BuildAvaloniaTask))]
    public sealed class PackageAvaloniaTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            foreach (var runtimeIdentifier in BuildContext.AvaloniaRuntimeIdentifiers)
            {
                var bin = new DirectoryPath(BuildContext.AvaloniaResultsPathFor(runtimeIdentifier));

                var files = context.FileSystem
                    .GetDirectory(bin)
                    .GetFiles("*", SearchScope.Current)
                    .Select(p => p.Path)
                    .ToList();

                if (runtimeIdentifier.StartsWith("linux"))
                {
                    // zip can't carry the executable bit, so linux gets a tar.gz
                    CreateTarGz(context, bin.CombineWithFilePath(new FilePath($"Dawn_{context.SemVer2}_{runtimeIdentifier}.tar.gz")), files);
                }
                else
                {
                    context.ZipCompress(bin, bin.CombineWithFilePath(new FilePath($"Dawn_{context.SemVer2}_{runtimeIdentifier}.zip")), files);
                }

                foreach (var file in files)
                {
                    context.DeleteFile(file);
                }
            }
        }

        private static void CreateTarGz(BuildContext context, FilePath archive, System.Collections.Generic.IEnumerable<FilePath> files)
        {
            const UnixFileMode executable = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
            const UnixFileMode regular = UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.GroupRead
                | UnixFileMode.OtherRead;

            var archivePath = context.MakeAbsolute(archive).FullPath;

            using var output = File.Create(archivePath);
            using var gzip = new GZipStream(output, CompressionLevel.Optimal);
            using var writer = new TarWriter(gzip);

            foreach (var file in files)
            {
                var path = context.MakeAbsolute(file).FullPath;
                var name = file.GetFilename().FullPath;

                // the self-contained single-file app is the only file that isn't a symbol file
                var entry = new PaxTarEntry(TarEntryType.RegularFile, name)
                {
                    Mode = name.EndsWith(".pdb") ? regular : executable,
                    DataStream = File.OpenRead(path),
                };

                using (entry.DataStream)
                {
                    writer.WriteEntry(entry);
                }
            }
        }
    }
}
