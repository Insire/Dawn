using Cake.Common;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    [TaskName("GenerateLicenseFile")]
    public sealed class GenerateLicenseFileTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (context.Environment.Platform.IsWindows())
            {
                RunForWpfApp();
            }

            RunForAvaloniaApp();

            void RunForWpfApp()
            {
                var settings = new ProcessSettings()
                    .UseWorkingDirectory(context.Environment.WorkingDirectory)
                    .WithArguments(builder => builder
                        .Append("nuget-license")
                        .AppendSwitchQuoted("-i", context.WpfProjectFilePath.FullPath)
                        .AppendSwitch("-o", "json")
                        .AppendSwitchQuoted("-fo", context.WpfLicenseFilePath.FullPath)
                    );

                context.StartProcess("dotnet", settings);
            }

            void RunForAvaloniaApp()
            {
                var settings = new ProcessSettings()
                    .UseWorkingDirectory(context.Environment.WorkingDirectory)
                    .WithArguments(builder => builder
                        .Append("nuget-license")
                        .AppendSwitchQuoted("-i", context.AvaloniaProjectFilePath.FullPath)
                        .AppendSwitch("-o", "json")
                        .AppendSwitchQuoted("-fo", context.AvaloniaLicenseFilePath.FullPath)
                    );

                context.StartProcess("dotnet", settings);
            }
        }
    }
}
