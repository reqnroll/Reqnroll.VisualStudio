using Reqnroll.Bindings;

namespace ReqnrollConnector.ReqnrollProxies;

public class ReqnrollDependencyProviderBeforeV307013 : ReqnrollDependencyProviderBeforeV309022
{
    public ReqnrollDependencyProviderBeforeV307013(AssemblyLoadContext loadContext) : base(loadContext)
    {
    }

    protected override void RegisterBindingInvoker(ObjectContainer container)
    {
        container.ReflectionRegisterTypeAs<NullBindingInvoker, IBindingInvoker>();
    }
}
