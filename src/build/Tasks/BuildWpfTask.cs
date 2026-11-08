using Cake.Common;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    [TaskName("BuildWpf")]
    [IsDependentOn(typeof(RestoreSolutionTask))]
    [IsDependentOn(typeof(UpdateAssemblyInfoTask))]
    [IsDependentOn(typeof(GenerateLicenseFileTask))]
    public sealed class BuildWpfTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var settings = new ProcessSettings()
                .UseWorkingDirectory(".")
                .WithArguments(builder => builder
                    .Append("publish")
                    .AppendQuoted(context.WpfProjectFilePath.FullPath)
                    .Append("--nologo")
                    .Append($"-c {BuildContext.BuildConfiguration}")
                    .Append($"-r {BuildContext.WpfRuntimeIdentifier}")
                    .Append("--no-restore")
                    .Append("-p:EnableWindowsTargeting=true") // allows publishing from non-Windows hosts
                    .Append($"--output \"{BuildContext.WpfResultsPath}\"")
                    .Append("--self-contained true")
                    .Append("-p:IncludeAllContentForSelfExtract=true")
                    .Append("-p:PublishSingleFile=true")
                    .Append("-p:PublishTrimmed=false")
                    .Append("-p:PublishReadyToRun=false")
                    .Append("-p:PublicRelease=true") // Nerdbank.GitVersioning - omit git commit ID
                );

            context.StartProcessOrThrow("dotnet", settings);
        }
    }
}
