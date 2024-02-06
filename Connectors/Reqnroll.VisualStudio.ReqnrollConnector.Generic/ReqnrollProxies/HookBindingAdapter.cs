using Reqnroll.Bindings.Provider.Data;

namespace ReqnrollConnector.ReqnrollProxies;

public record HookBindingAdapter(HookData Adaptee) : IScopedBindingAdapter
{
    public string HookType => Adaptee.Type;
    public BindingMethodAdapter Method { get; } = new(Adaptee.Source?.Method);
    public bool IsScoped => Adaptee.Scope != null;
    public string? BindingScopeTag => Adaptee.Scope?.Tag;
    public string? BindingScopeFeatureTitle => Adaptee.Scope?.FeatureTitle;
    public string? BindingScopeScenarioTitle => Adaptee.Scope?.ScenarioTitle;
    public int? HookOrder => Adaptee.HookOrder;
}