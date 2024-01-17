#nullable disable

using SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V38;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V40;

public class SpecFlowV40Discoverer : SpecFlowV38Discoverer
{
    public SpecFlowV40Discoverer(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override void EnsureTestRunnerCreated(TestRunnerManager testRunnerManager)
    {
        //testRunnerManager.CreateTestRunner("default");
        testRunnerManager.ReflectionCallMethod(nameof(TestRunnerManager.CreateTestRunner), new[] { typeof(string) }, "default");
    }
}
