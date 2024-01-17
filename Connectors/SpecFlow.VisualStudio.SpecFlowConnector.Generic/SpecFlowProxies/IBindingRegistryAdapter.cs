namespace SpecFlowConnector.SpecFlowProxies;

public interface IBindingRegistryAdapter
{
    IEnumerable<StepDefinitionBindingAdapter> GetStepDefinitions();
}
