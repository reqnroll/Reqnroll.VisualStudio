namespace ReqnrollConnector.ReqnrollProxies;

public interface IBindingRegistryAdapter
{
    IEnumerable<StepDefinitionBindingAdapter> GetStepDefinitions();
    IEnumerable<HookBindingAdapter> GetHooks();
}