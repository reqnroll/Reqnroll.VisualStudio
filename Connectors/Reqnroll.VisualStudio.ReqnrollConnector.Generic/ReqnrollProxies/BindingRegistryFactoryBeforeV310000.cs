using Reqnroll;

namespace ReqnrollConnector.ReqnrollProxies;

public class BindingRegistryFactoryBeforeV310000 : BindingRegistryFactoryVLatest
{
    public BindingRegistryFactoryBeforeV310000(ILogger log) : base(log)
    {
    }

    protected override ITestRunner InvokeCreateTestRunner(TestRunnerManager testRunnerManager)
    {
        return testRunnerManager.ReflectionCallMethod<ITestRunner>(nameof(TestRunnerManager.CreateTestRunner), 0);
    }
}