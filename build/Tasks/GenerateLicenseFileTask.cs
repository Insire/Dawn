using Cake.Common;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    [TaskName("GenerateLicenseFile")]
    public sealed class GenerateLicenseFileTask : FrostingTask<BuildContext>
    {
        private const string DotnetToolName = "nuget-license";
        private const string DotnetToolVersion = "3.1.3";

        public override void Run(BuildContext context)
        {
            Install();
            RunForWpfApp();
            RunForAvaloniaApp();
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

            void RunForWpfApp()
            {
                var settings = new ProcessSettings()
                    .UseWorkingDirectory(".")
                    .WithArguments(builder => builder
                        .AppendSwitchQuoted("-i", BuildContext.WpfProjectFilePath)
                        .AppendSwitch("-o", "json")
                        .AppendSwitchQuoted("-fo", BuildContext.WpfLicenseFilePath)
                    );

                context.StartProcess(DotnetToolName, settings);
            }

            void RunForAvaloniaApp()
            {
                var settings = new ProcessSettings()
                    .UseWorkingDirectory(".")
                    .WithArguments(builder => builder
                        .AppendSwitchQuoted("-i", BuildContext.AvaloniaProjectFilePath)
                        .AppendSwitch("-o", "json")
                        .AppendSwitchQuoted("-fo", BuildContext.AvaloniaLicenseFilePath)
                    );

                context.StartProcess(DotnetToolName, settings);
            }
        }
    }
}
