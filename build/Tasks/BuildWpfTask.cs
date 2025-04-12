using Cake.Common;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    [TaskName("BuildWpf")]
    public sealed class BuildWpfTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var settings = new ProcessSettings()
                .UseWorkingDirectory(".")
                .WithArguments(builder => builder
                    .Append("publish")
                    .AppendQuoted(BuildContext.WpfProjectFilePath)
                    .Append("--nologo")
                    .Append($"-c {BuildContext.BuildConfiguration}")
                    .Append("-r win-x64")
                    .Append($"--output \"{BuildContext.WpfResultsPath}\"")
                    .Append("--self-contained true")
                    .Append("-p:IncludeAllContentForSelfExtract=true")
                    .Append("-p:PublishSingleFile=true")
                    .Append("-p:PublishTrimmed=false")
                    .Append("-p:PublishReadyToRun=false")
                    .Append("-p:PublicRelease=true") // Nerdbank.GitVersioning - omit git commit ID
                );

            context.StartProcess("dotnet", settings);
        }
    }
}
