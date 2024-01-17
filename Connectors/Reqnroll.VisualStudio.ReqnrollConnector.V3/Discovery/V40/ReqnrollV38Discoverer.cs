#nullable disable

using Reqnroll.VisualStudio.ReqnrollConnector.Discovery.V38;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery.V40;

public class ReqnrollV40Discoverer : ReqnrollV38Discoverer
{
    public ReqnrollV40Discoverer(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override void EnsureTestRunnerCreated(TestRunnerManager testRunnerManager)
    {
        //testRunnerManager.CreateTestRunner("default");
        testRunnerManager.ReflectionCallMethod(nameof(TestRunnerManager.CreateTestRunner), new[] { typeof(string) }, "default");
    }
}
