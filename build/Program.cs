using Cake.Frosting;
using System;

namespace Build
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .UseWorkingDirectory("..")
                .InstallTool(new Uri("dotnet:?package=GitVersion.Tool&version=6.2.0"))
                .InstallTool(new Uri("dotnet:?package=nuget-license&version=3.1.3"))
                .Run(args);
        }
    }
}
