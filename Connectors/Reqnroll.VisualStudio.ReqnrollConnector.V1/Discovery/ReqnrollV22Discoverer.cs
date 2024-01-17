namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

public class ReqnrollV22Discoverer : ReqnrollV30Discoverer
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
