using Cake.Common.IO;
using Cake.Compression;
using Cake.Core.IO;
using Cake.Frosting;
using System.Linq;

namespace Build
{
    [TaskName("Package")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackageTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var bin = new DirectoryPath(BuildContext.ResultsPath);

            var files = context.FileSystem
                .GetDirectory(bin)
                .GetFiles("*", SearchScope.Current)
                .Select(p => p.Path);

            context.ZipCompress(bin, bin.CombineWithFilePath(new FilePath($".\\Dawn_{context.GitVersion.SemVer2}.zip")), files);

            foreach (var file in files)
            {
                context.DeleteFile(file);
            }
        }
    }
}
