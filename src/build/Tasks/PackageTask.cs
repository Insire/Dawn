using Cake.Frosting;

namespace Build
{
    [TaskName("Package")]
    [IsDependentOn(typeof(BuildTask))]
    [IsDependentOn(typeof(PackageWpfTask))]
    [IsDependentOn(typeof(PackageAvaloniaTask))]
    public sealed class PackageTask : FrostingTask<BuildContext>
    {
    }
}
