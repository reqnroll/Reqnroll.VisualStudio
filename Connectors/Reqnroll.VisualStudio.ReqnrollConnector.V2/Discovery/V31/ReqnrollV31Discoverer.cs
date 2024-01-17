namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery.V31;

public class ReqnrollV31Discoverer : ReqnrollV3BaseDiscoverer
{
    public ReqnrollV31Discoverer(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override DefaultDependencyProvider CreateDefaultDependencyProvider() =>
        new ReqnrollV31DependencyProvider(_loadContext);

    private class ReqnrollV31DependencyProvider : NoInvokeDependencyProvider
    {
        private readonly AssemblyLoadContext _loadContext;

        public ReqnrollV31DependencyProvider(AssemblyLoadContext loadContext)
        {
            _loadContext = loadContext;
        }

        public override void RegisterGlobalContainerDefaults(ObjectContainer globalContainer)
        {
            base.RegisterGlobalContainerDefaults(globalContainer);
            globalContainer.RegisterInstanceAs(_loadContext);

            var pluginLoaderType = new DynamicRuntimePluginLoaderFactory().Create();
            globalContainer.ReflectionRegisterTypeAs(pluginLoaderType, typeof(IRuntimePluginLoader));
        }
    }
}
