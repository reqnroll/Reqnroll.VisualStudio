using TechTalk.SpecFlow.Bindings;

namespace SpecFlowConnector.SpecFlowProxies;

public class SpecFlowDependencyProviderBeforeV307013 : SpecFlowDependencyProviderBeforeV309022
{
    public SpecFlowDependencyProviderBeforeV307013(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override void RegisterBindingInvoker(ObjectContainer container)
    {
        container.ReflectionRegisterTypeAs<NullBindingInvoker, IBindingInvoker>();
    }
}
