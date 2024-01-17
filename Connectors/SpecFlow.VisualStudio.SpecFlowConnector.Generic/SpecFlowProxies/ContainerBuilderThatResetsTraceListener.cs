using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlowConnector.SpecFlowProxies;

public class ContainerBuilderThatResetsTraceListener : ContainerBuilder
{
    public ContainerBuilderThatResetsTraceListener(IDefaultDependencyProvider defaultDependencyProvider)
        : base(defaultDependencyProvider)
    {
    }

    public override IObjectContainer CreateTestThreadContainer(IObjectContainer globalContainer)
    {
        var testThreadContainer = base.CreateTestThreadContainer(globalContainer);
        testThreadContainer.ReflectionRegisterTypeAs<NullListener, ITraceListener>();
        return testThreadContainer;
    }
}
