using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace SpecFlowConnector.SpecFlowProxies;

public class NoInvokeDependencyProvider : DefaultDependencyProvider
{
    public override void RegisterGlobalContainerDefaults(ObjectContainer container)
    {
        BaseRegisterGlobalContainerDefaults(container);
        RegisterBindingInvoker(container);
    }

    protected virtual void BaseRegisterGlobalContainerDefaults(ObjectContainer container)
    {
        base.RegisterGlobalContainerDefaults(container);
    }

    protected virtual void RegisterBindingInvoker(ObjectContainer container)
    {
        container.RegisterTypeAs<NullBindingInvoker, IBindingInvoker>();
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    protected class NullBindingInvoker : IBindingInvoker
    {
        public object InvokeBinding(IBinding binding, IContextManager contextManager, object[] arguments,
            ITestTracer testTracer, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;
            return null!;
        }
    }
}
