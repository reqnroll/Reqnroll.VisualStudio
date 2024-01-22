using Reqnroll.Bindings.Provider.Data;

namespace ReqnrollConnector.ReqnrollProxies;

public record StepDefinitionBindingAdapter(StepDefinitionData Adaptee)
{
    public string StepDefinitionType => Adaptee.Type;
    public string[] ParamTypes => Adaptee.ParamTypes;
    public Option<string> Regex => Adaptee.Regex;
    public BindingMethodAdapter Method { get; } = new(Adaptee.Source?.Method);
    public bool IsScoped => Adaptee.Scope != null;
    public Option<string> BindingScopeTag => Adaptee.Scope?.Tag;
    public string? BindingScopeFeatureTitle => Adaptee.Scope?.FeatureTitle;
    public string? BindingScopeScenarioTitle => Adaptee.Scope?.ScenarioTitle;
    public virtual Option<T> GetProperty<T>(string propertyName)
    {
        return Adaptee.ReflectionHasProperty(propertyName) ? Adaptee.ReflectionGetProperty<T>(propertyName) : None<T>.Value;
    }
}
