using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.GitVersioning;
using Nerdbank.GitVersioning;

namespace Build
{
    public sealed class BuildContext : FrostingContext
    {
        public const string Platform = "AnyCPU";
        public const string BuildConfiguration = "Release";

        public DirectoryPath WpfProjectFolderPath
            => Environment.WorkingDirectory
            .Combine("src")
            .Combine("Dawn")
            .Combine("Dawn.Wpf");

        public DirectoryPath AvaloniaProjectFolderPath
            => Environment.WorkingDirectory
            .Combine("src")
            .Combine("Dawn")
            .Combine("Dawn.Avalonia");

        public FilePath WpfProjectFilePath
            => WpfProjectFolderPath.CombineWithFilePath(@"Dawn.Wpf.csproj");

        public FilePath AvaloniaProjectFilePath
            => AvaloniaProjectFolderPath.CombineWithFilePath("Dawn.Avalonia.csproj");

        public FilePath WpfLicenseFilePath
            => WpfProjectFolderPath.Combine("Properties").CombineWithFilePath("licenses.json");

        public FilePath AvaloniaLicenseFilePath
            => AvaloniaProjectFolderPath.Combine("Properties").CombineWithFilePath("licenses.json");

        public const string AssemblyInfoPath = @".\src\Dawn\SharedAssemblyInfo.cs";

        public const string WpfResultsPath = ResultsPath + "/wpf";
        public const string AvaloniaResultsPath = ResultsPath + "/avalonia";
        public const string ResultsPath = "./binaries";

        private VersionOracle GitVersion { get; }

        public string SemVer2 => GitVersion.SemVer2;

        public BuildContext(ICakeContext context)
            : base(context)
        {
            GitVersion = context.GitVersioningGetVersion();

            this.Information($"Provider: {context.BuildSystem().Provider}");
            this.Information($"Platform: {context.Environment.Platform.Family} ({(context.Environment.Platform.Is64Bit ? "x64" : "x86")})");

            this.Information($"Version: {SemVer2}");
        }
    }
}
