#nullable disable
using SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V31;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V38;

public class SpecFlowV38Discoverer : SpecFlowV31Discoverer
{
    public SpecFlowV38Discoverer(AssemblyLoadContext loadContext) : base(loadContext)
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
