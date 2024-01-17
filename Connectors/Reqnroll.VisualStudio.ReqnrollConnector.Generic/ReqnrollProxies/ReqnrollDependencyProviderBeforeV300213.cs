using Reqnroll.Bindings;

namespace ReqnrollConnector.ReqnrollProxies;

public class ReqnrollDependencyProviderBeforeV300213 : ReqnrollDependencyProviderBeforeV307013
{
    public ReqnrollDependencyProviderBeforeV300213(AssemblyLoadContext loadContext) : base(loadContext)
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
