#nullable disable
using Reqnroll.VisualStudio.ReqnrollConnector.Discovery.V31;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery.V38;

public class ReqnrollV38Discoverer : ReqnrollV31Discoverer
{
    public ReqnrollV38Discoverer(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override ContainerBuilder CreateContainerBuilder(DefaultDependencyProvider defaultDependencyProvider) =>
        new ContainerBuilderThatResetsTraceListener(defaultDependencyProvider);

    private class ContainerBuilderThatResetsTraceListener : ContainerBuilder
    {
        public ContainerBuilderThatResetsTraceListener(IDefaultDependencyProvider defaultDependencyProvider = null) :
            base(defaultDependencyProvider)
        {
        }

        public override IObjectContainer CreateTestThreadContainer(IObjectContainer globalContainer)
        {
            var testThreadContainer = base.CreateTestThreadContainer(globalContainer);
            testThreadContainer.ReflectionRegisterTypeAs<NullListener, ITraceListener>();
            return testThreadContainer;
        }
    }
}
