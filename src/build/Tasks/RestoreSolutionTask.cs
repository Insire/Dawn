using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Restore;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    [TaskName("RestoreSolution")]
    [IsDependentOn(typeof(CleanSolutionTask))]
    public sealed class RestoreSolutionTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var settings = new DotNetRestoreSettings
            {
                MSBuildSettings = new DotNetMSBuildSettings().WithProperty("EnableWindowsTargeting", "true"),
            };

            context.DotNetRestore(context.Environment.WorkingDirectory.CombineWithFilePath("Dawn.sln").FullPath, settings);
        }
    }
}
