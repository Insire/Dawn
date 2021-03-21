using Cake.Frosting;

namespace Build
{
    [TaskName("CleanSolution")]
    public sealed class CleanSolutionTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.Clean(true, true, true, true);
        }
    }
}
