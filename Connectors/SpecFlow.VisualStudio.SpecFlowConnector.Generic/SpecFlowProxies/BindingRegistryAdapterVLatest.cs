using TechTalk.SpecFlow.Bindings;

namespace SpecFlowConnector.SpecFlowProxies;

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
