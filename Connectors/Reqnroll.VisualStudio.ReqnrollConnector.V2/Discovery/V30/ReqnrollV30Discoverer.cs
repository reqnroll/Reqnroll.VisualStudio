using System;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery.V30;

public class ReqnrollV30Discoverer : ReqnrollV3BaseDiscoverer
{
    public ReqnrollV30Discoverer(AssemblyLoadContext loadContext) : base(loadContext)
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
