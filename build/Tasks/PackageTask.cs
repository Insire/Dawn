using Cake.Common.IO;
using Cake.Compression;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    [TaskName("Package")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackageTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var bin = new DirectoryPath(BuildContext.ResultsPath);

            context.ZipCompress(bin, bin.CombineWithFilePath(new FilePath($".\\Dawn_{context.GitVersion.SemVer2}.zip")), new FilePath[]
            {
                bin.CombineWithFilePath(new FilePath(".\\Dawn.Wpf.exe")),
                bin.CombineWithFilePath(new FilePath(".\\Dawn.Wpf.pdb"))
            });

            context.DeleteFile(bin.CombineWithFilePath(new FilePath(".\\Dawn.Wpf.exe")));
            context.DeleteFile(bin.CombineWithFilePath(new FilePath(".\\Dawn.Wpf.pdb")));
        }
    }
}
