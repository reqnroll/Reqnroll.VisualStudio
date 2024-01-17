using Reqnroll.Bindings;

namespace ReqnrollConnector.ReqnrollProxies;

public class BindingRegistryAdapterBeforeV20100 : BindingRegistryAdapterVLatest
{
    public BindingRegistryAdapterBeforeV20100(IBindingRegistry adaptee) : base(adaptee)
    {
    }

    public override IEnumerable<StepDefinitionBindingAdapter> GetStepDefinitions() =>
        _adaptee.GetConsideredStepDefinitions(StepDefinitionType.Given)
            .Concat(_adaptee.GetConsideredStepDefinitions(StepDefinitionType.When))
            .Concat(_adaptee.GetConsideredStepDefinitions(StepDefinitionType.Then))
            .Select(Adapt);
}
