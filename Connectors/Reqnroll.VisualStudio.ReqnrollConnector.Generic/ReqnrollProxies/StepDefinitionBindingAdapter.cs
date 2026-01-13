using Reqnroll.Bindings.Provider.Data;

namespace ReqnrollConnector.ReqnrollProxies;

public interface IScopedBindingAdapter
{
    bool IsScoped { get; }
    string? BindingScopeTag { get; }
    string? BindingScopeFeatureTitle { get; }
    string? BindingScopeScenarioTitle { get; }
    string? BindingScopeError { get; }
}

public record StepDefinitionBindingAdapter(StepDefinitionData Adaptee) : IScopedBindingAdapter
{
    public string StepDefinitionType => Adaptee.Type;
    public string[] ParamTypes => Adaptee.ParamTypes;
    public string? Regex => Adaptee.Regex;
    public string? Expression => Adaptee.Expression;
    public string? Error => Adaptee.Error;
    public BindingMethodAdapter Method { get; } = new(Adaptee.Source?.Method);
    public bool IsScoped => Adaptee.Scope != null;
    public string? BindingScopeTag => Adaptee.Scope?.Tag;
    public string? BindingScopeFeatureTitle => Adaptee.Scope?.FeatureTitle;
    public string? BindingScopeScenarioTitle => Adaptee.Scope?.ScenarioTitle;
    public string? BindingScopeError => Adaptee.Scope?.Error;
    public virtual Option<T> GetProperty<T>(string propertyName)
    {
        return Adaptee.ReflectionHasProperty(propertyName) ? Adaptee.ReflectionGetProperty<T>(propertyName) : None<T>.Value;
    }
}
