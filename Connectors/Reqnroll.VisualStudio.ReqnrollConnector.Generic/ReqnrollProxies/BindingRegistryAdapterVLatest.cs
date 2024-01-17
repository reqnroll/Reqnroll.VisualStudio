using Reqnroll.Bindings;

namespace ReqnrollConnector.ReqnrollProxies;

public class BindingRegistryAdapterVLatest : IBindingRegistryAdapter
{
    protected readonly IBindingRegistry _adaptee;


    public BindingRegistryAdapterVLatest(IBindingRegistry adaptee)
    {
        _adaptee = adaptee;
    }

    public virtual IEnumerable<StepDefinitionBindingAdapter> GetStepDefinitions() =>
        _adaptee
            .GetStepDefinitions()
            .Select(Adapt);

    protected StepDefinitionBindingAdapter Adapt(IStepDefinitionBinding sd) => new(sd);
}
