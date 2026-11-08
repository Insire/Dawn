using Cake.Frosting;

namespace Build
{
    [TaskName("Build")]
    [IsDependentOn(typeof(RestoreSolutionTask))]
    [IsDependentOn(typeof(UpdateAssemblyInfoTask))]
    [IsDependentOn(typeof(GenerateLicenseFileTask))]
    [IsDependentOn(typeof(BuildWpfTask))]
    [IsDependentOn(typeof(BuildAvaloniaTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
    }
}
