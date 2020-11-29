#tool nuget:?package=GitVersion.CommandLine&version=5.3.7

#addin nuget:?package=Cake.Incubator&version=5.1.0
#addin nuget:?package=Cake.GitVersioning&version=3.2.31
#addin "nuget:?package=SharpZipLib&version=1.2.0"
#addin "nuget:?package=Cake.Compression&version=0.2.4"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

const string Platform = "AnyCPU";
const string SolutionPath = @".\src\Dawn\Dawn.sln";
const string AssemblyInfoPath = @".\src\Dawn\SharedAssemblyInfo.cs";
const string BinPath = "./binaries/";

var target = Argument("target", "Default");
var Configuration = Argument("configuration", "Release");
var semver = "";
var gitVersion = default(GitVersion);

private void Build(string path)
{
    var settings = new ProcessSettings()
        .UseWorkingDirectory(".")
        .WithArguments(builder => builder
            .Append("publish")
            .AppendQuoted(path)
            .Append("--nologo")
            .Append($"-c {Configuration}")
            .Append($"--output \"{BinPath}\"")
    );

    StartProcess("dotnet", settings);
}

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    Debug("IsLocalBuild: " + BuildSystem.IsLocalBuild);
    Debug("IsRunningOnAppVeyor: " + BuildSystem.IsRunningOnAppVeyor);
    Debug("IsRunningOnAzurePipelines: " + BuildSystem.IsRunningOnAzurePipelines);
    Debug("IsRunningOnAzurePipelinesHosted: " + BuildSystem.IsRunningOnAzurePipelinesHosted);

    Information("Provider: " + BuildSystem.Provider);

    foreach(var entry in Context.EnvironmentVariables())
    {
        Debug(entry.Key + " " + entry.Value);
    }

    gitVersion = GitVersion();
    semver = GitVersioningGetVersion().SemVer2;
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("CleanSolution")
    .Does(() =>
    {
        var solution = ParseSolution(SolutionPath);

        foreach(var project in solution.Projects)
        {
            // check solution items and exclude solution folders, since they are virtual
            if(project.Name == "Solution Items")
                continue;

            var customProject = ParseProject(project.Path, configuration: Configuration, platform: Platform);

            foreach(var path in customProject.OutputPaths)
            {
                CleanDirectory(path.FullPath);
            }
        }

        var folders = new[]
        {
            new DirectoryPath(BinPath),
        };

        foreach(var folder in folders)
        {
            EnsureDirectoryExists(folder);
            CleanDirectory(folder,(file) => !file.Path.Segments.Last().Contains(".gitignore"));
        }
    });

Task("UpdateAssemblyInfo")
    .Does(() =>
    {
        var assemblyInfoParseResult = ParseAssemblyInfo(AssemblyInfoPath);

        var settings = new AssemblyInfoSettings()
        {
            Product                 = assemblyInfoParseResult.Product,
            Company                 = assemblyInfoParseResult.Company,
            Trademark               = assemblyInfoParseResult.Trademark,
            Copyright               = $"© {DateTime.Today.Year} Insire",

            InternalsVisibleTo      = assemblyInfoParseResult.InternalsVisibleTo,

            MetaDataAttributes = new []
            {
                new AssemblyInfoMetadataAttribute()
                {
                    Key = "Platform",
                    Value = Platform,
                },
                new AssemblyInfoMetadataAttribute()
                {
                    Key = "CompileDate",
                    Value = "[UTC]" + DateTime.UtcNow.ToString(),
                },
                new AssemblyInfoMetadataAttribute()
                {
                    Key = "Branch",
                    Value = gitVersion.BranchName,
                },
                new AssemblyInfoMetadataAttribute()
                {
                    Key = "Commit",
                    Value = gitVersion.Sha,
                },
                new AssemblyInfoMetadataAttribute()
                {
                    Key = "Version",
                    Value = semver,
                },
            }
        };

        CreateAssemblyInfo(new FilePath(AssemblyInfoPath), settings);
    });

Task("GenerateLicenseFile")
    .Does(() =>
    {
        Install();
        Run();
        Uninstall();

        void Install()
        {
            var settings = new ProcessSettings()
                .UseWorkingDirectory(".")
                .WithArguments(builder => builder
                    .Append("tool install --global dotnet-project-licenses --version 2.2.6")
            );

            StartProcess("dotnet", settings);
        }

        void Uninstall()
        {
            var settings = new ProcessSettings()
                .UseWorkingDirectory(".")
                .WithArguments(builder => builder
                    .Append("tool uninstall --global dotnet-project-licenses")
            );

            StartProcess("dotnet", settings);
        }

        void Run()
        {
            var settings = new ProcessSettings()
                .UseWorkingDirectory(".")
                .WithArguments(builder => builder
                    .AppendSwitchQuoted("-i", @".\src\Dawn\Dawn.Wpf")
                    .Append("-j")
                );

            StartProcess("dotnet-project-licenses", settings);
        }
    });

Task("BuildAndPack")
    .Does(() =>
    {
        Build(@".\src\Dawn\Dawn.Wpf\Dawn.Wpf.csproj");

        var bin = new DirectoryPath(BinPath);

        ZipCompress(bin, bin.CombineWithFilePath(new FilePath($".\\Dawn_{semver}.zip")), new FilePath[]
        {
            bin.CombineWithFilePath(new FilePath(".\\Dawn.Wpf.exe")),
            bin.CombineWithFilePath(new FilePath(".\\Dawn.Wpf.pdb"))
        });

        DeleteFile(bin.CombineWithFilePath(new FilePath(".\\Dawn.Wpf.exe")));
        DeleteFile(bin.CombineWithFilePath(new FilePath(".\\Dawn.Wpf.pdb")));
    });

Task("Default")
    .IsDependentOn("CleanSolution")
    .IsDependentOn("UpdateAssemblyInfo")
    .IsDependentOn("GenerateLicenseFile")
    .IsDependentOn("BuildAndPack");

RunTarget(target);
