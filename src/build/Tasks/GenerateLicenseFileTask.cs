using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using System.Collections.Generic;

namespace Build
{
    [TaskName("GenerateLicenseFile")]
    [IsDependentOn(typeof(RestoreSolutionTask))]
    public sealed class GenerateLicenseFileTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            Generate(context, context.WpfProjectFilePath, context.WpfLicenseFilePath);
            Generate(context, context.AvaloniaProjectFilePath, context.AvaloniaLicenseFilePath);
        }

        private static void Generate(BuildContext context, FilePath project, FilePath output)
        {
            var settings = new ProcessSettings
            {
                // nuget-license would otherwise pick the .NET 9 SDK, which can't load WPF projects on non-Windows hosts
                EnvironmentVariables = new Dictionary<string, string> { ["DOTNET_ROLL_FORWARD"] = "LatestMajor" },
            }
                .UseWorkingDirectory(context.Environment.WorkingDirectory)
                .WithArguments(builder => builder
                    .Append("nuget-license")
                    .AppendSwitchQuoted("-i", project.FullPath)
                    .AppendSwitch("-o", "json")
                    .AppendSwitchQuoted("-fo", output.FullPath)
                );

            // exit code 2: some packages have no usable license metadata; the output file is still written
            if (context.StartProcessOrThrow("dotnet", settings, 2) == 2)
            {
                context.Warning($"nuget-license reported packages without usable license information for {project.GetFilename()}");
            }
        }
    }
}
