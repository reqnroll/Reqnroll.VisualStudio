namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class SpecFlowV22Discoverer : SpecFlowV30Discoverer
{
    protected override object CreateGlobalContainer(Assembly testAssembly,
        IRuntimeConfigurationProvider configurationProvider, IContainerBuilder containerBuilder)
    {
        var globalContainer = containerBuilder.ReflectionCallMethod<object>(
            nameof(ContainerBuilder.CreateGlobalContainer),
            configurationProvider);

        return globalContainer;
    }
}
