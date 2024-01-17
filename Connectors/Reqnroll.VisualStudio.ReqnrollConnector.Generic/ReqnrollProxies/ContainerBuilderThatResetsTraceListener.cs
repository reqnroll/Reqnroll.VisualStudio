using Reqnroll.Infrastructure;
using Reqnroll.Tracing;

namespace ReqnrollConnector.ReqnrollProxies;

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
