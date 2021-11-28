using Cake.Common;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    [TaskName("GenerateLicenseFile")]
    public sealed class GenerateLicenseFileTask : FrostingTask<BuildContext>
    {
        private const string DotnetToolName = "dotnet-project-licenses";
        private const string DotnetToolVersion = "2.3.6";

        public override void Run(BuildContext context)
        {
            Install();
            Run();
            Uninstall();

            void Install()
            {
                var settings = new ProcessSettings()
                    .UseWorkingDirectory(".")
                    .WithArguments(builder => builder
                        .Append($"tool install --global {DotnetToolName} --version {DotnetToolVersion}")
                );

                context.StartProcess("dotnet", settings);
            }

            void Uninstall()
            {
                var settings = new ProcessSettings()
                    .UseWorkingDirectory(".")
                    .WithArguments(builder => builder
                        .Append($"tool uninstall --global {DotnetToolName} ")
                );

                context.StartProcess("dotnet", settings);
            }

            void Run()
            {
                var settings = new ProcessSettings()
                    .UseWorkingDirectory(".")
                    .WithArguments(builder => builder
                        .AppendSwitchQuoted("-i", BuildContext.ProjectFolderPath)
                        .Append("-j")
                    );

                context.StartProcess(DotnetToolName, settings);
            }
        }
    }
}
