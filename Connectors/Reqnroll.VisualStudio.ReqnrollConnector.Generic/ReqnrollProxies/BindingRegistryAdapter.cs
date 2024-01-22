using Reqnroll.Bindings.Provider.Data;

namespace ReqnrollConnector.ReqnrollProxies;

public record BindingRegistryAdapter(BindingData Adaptee) : IBindingRegistryAdapter
{
    public IEnumerable<StepDefinitionBindingAdapter> GetStepDefinitions()
    {
        return (Adaptee.StepDefinitions ?? Array.Empty<StepDefinitionData>()).Select(sd => new StepDefinitionBindingAdapter(sd));
    }
}