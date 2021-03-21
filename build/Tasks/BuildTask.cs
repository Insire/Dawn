using Cake.Common;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    [TaskName("Build")]
    [IsDependentOn(typeof(CleanSolutionTask))]
    [IsDependentOn(typeof(UpdateAssemblyInfoTask))]
    [IsDependentOn(typeof(GenerateLicenseFileTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var settings = new ProcessSettings()
                .UseWorkingDirectory(".")
                .WithArguments(builder => builder
                    .Append("publish")
                    .AppendQuoted(BuildContext.SolutionPath)
                    .Append("--nologo")
                    .Append($"-c {BuildContext.BuildConfiguration}")
                    .Append($"--output \"{BuildContext.ResultsPath}\"")
                    .Append($"-p:PublicRelease=true") // Nerdbank.GitVersioning - omit git commit ID
                );

            context.StartProcess("dotnet", settings);
        }
    }
}
