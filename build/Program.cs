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
                .InstallTool(new Uri("nuget:?package=GitVersion.CommandLine&version=5.8.1"))
                .Run(args);
        }
    }
}
