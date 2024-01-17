using System;

namespace SpecFlow.VisualStudio.SpecFlowConnector.Discovery.V30;

public class SpecFlowV30Discoverer : SpecFlowV3BaseDiscoverer
{
    public SpecFlowV30Discoverer(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override IObjectContainer CreateGlobalContainer(IConfigurationLoader configurationLoader,
        Assembly testAssembly)
    {
        // We need to call the CreateGlobalContainer through reflection because the interface has been changed in V3.0.220.

        var containerBuilder = new ContainerBuilder(new NoInvokeDependencyProvider());
        var configurationProvider = new DefaultRuntimeConfigurationProvider(configurationLoader);

        //var globalContainer = containerBuilder.CreateGlobalContainer(
        //        new DefaultRuntimeConfigurationProvider(configurationLoader));
        var globalContainer =
            containerBuilder.ReflectionCallMethod<object>(nameof(ContainerBuilder.CreateGlobalContainer),
                configurationProvider);
        return (IObjectContainer) globalContainer;
    }
}
