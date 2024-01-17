namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery;

public class SpecFlowV30Discoverer : SpecFlowVLatestDiscoverer
{
    protected override object CreateGlobalContainer(Assembly testAssembly,
        IRuntimeConfigurationProvider configurationProvider, IContainerBuilder containerBuilder)
    {
        var globalContainer = containerBuilder.ReflectionCallMethod<object>(
            nameof(ContainerBuilder.CreateGlobalContainer),
            testAssembly, configurationProvider);

        return globalContainer;
    }

    protected override void RegisterTypeAs<TType, TInterface>(object globalContainer)
    {
        globalContainer.ReflectionRegisterTypeAs<TType, TInterface>();
    }

    protected override T Resolve<T>(object globalContainer) => globalContainer.ReflectionResolve<T>();
}
