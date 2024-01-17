using TechTalk.SpecFlow.Bindings;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlowDependencyProviderBeforeV300213 : SpecFlowDependencyProviderBeforeV307013
{
    public SpecFlowDependencyProviderBeforeV300213(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    public void RegisterGlobalContainerDefaults(object container)
    {
        RegisterBindingInvoker(container);
    }

    protected virtual void RegisterBindingInvoker(object container)
    {
        container.ReflectionRegisterTypeAs<NullBindingInvoker, IBindingInvoker>();
    }
}
