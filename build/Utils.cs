using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Incubator.Project;
using System.Linq;

namespace Build
{
    public static class Utils
    {
        public static void Clean(this BuildContext context, bool cleanBin, bool cleanObj, bool cleanOutput, bool cleanMisc)
        {
            var solution = context.ParseSolution(BuildContext.SolutionPath);

            foreach (var project in solution.Projects)
            {
                // check solution items and exclude solution folders, since they are virtual
                if (project.Type == "{2150E333-8FDC-42A3-9474-1A3956D46DE8}")
                {
                    continue;
                }

                var projectFile = project.Path; // FilePath
                if (cleanBin)
                {
                    var binFolder = projectFile.GetDirectory().Combine("bin");
                    if (context.DirectoryExists(binFolder))
                    {
                        context.CleanDirectory(binFolder);
                    }
                }

                if (cleanObj)
                {
                    var objFolder = projectFile.GetDirectory().Combine("obj");
                    if (context.DirectoryExists(objFolder))
                    {
                        context.CleanDirectory(objFolder);
                    }
                }

                if (cleanOutput)
                {
                    var customProject = context.ParseProject(project.Path, configuration: BuildContext.BuildConfiguration, platform: BuildContext.Platform);
                    foreach (var path in customProject.OutputPaths.Where(p => p != null))
                    {
                        context.CleanDirectory(path.FullPath);
                    }
                }
            }

            if (cleanMisc)
            {
                var folders = new[]
                {
                    BuildContext.ResultsPath,
                };

                foreach (var folder in folders)
                {
                    context.EnsureDirectoryExists(folder);
                    context.CleanDirectory(folder, (file) => !file.Path.Segments.Last().Contains(".gitignore"));
                }
            }
        }
    }
}
