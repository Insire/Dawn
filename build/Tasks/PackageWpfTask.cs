using Cake.Common.IO;
using Cake.Compression;
using Cake.Core.IO;
using Cake.Frosting;
using System.Linq;

namespace Build
{
    [TaskName("PackageWpf")]
    public sealed class PackageWpfTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var bin = new DirectoryPath(BuildContext.WpfResultsPath);

            var files = context.FileSystem
                .GetDirectory(bin)
                .GetFiles("*", SearchScope.Current)
                .Select(p => p.Path);

            context.ZipCompress(bin, bin.CombineWithFilePath(new FilePath($".\\Dawn_{context.SemVer2}.zip")), files);

            foreach (var file in files)
            {
                context.DeleteFile(file);
            }
        }
    }
}
