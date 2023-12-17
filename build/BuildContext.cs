using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Frosting;
using Cake.GitVersioning;
using Nerdbank.GitVersioning;

namespace Build
{
    public sealed class BuildContext : FrostingContext
    {
        public const string Platform = "AnyCPU";
        public const string BuildConfiguration = "Release";

        public const string ProjectFolderPath = @".\src\Dawn\Dawn.Wpf";
        public const string ProjectFilePath = @".\src\Dawn\Dawn.Wpf\Dawn.Wpf.csproj";
        public const string AssemblyInfoPath = @".\src\Dawn\SharedAssemblyInfo.cs";

        public const string ResultsPath = "./binaries";

        public VersionOracle GitVersion { get; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            GitVersion = context.GitVersioningGetVersion();

            this.Information($"Provider: {context.BuildSystem().Provider}");
            this.Information($"Platform: {context.Environment.Platform.Family} ({(context.Environment.Platform.Is64Bit ? "x64" : "x86")})");

            this.Information($"Version: {GitVersion.SemVer2}");
        }
    }
}
