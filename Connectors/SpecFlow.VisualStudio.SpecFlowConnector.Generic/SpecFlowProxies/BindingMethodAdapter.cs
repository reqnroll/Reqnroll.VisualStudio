using TechTalk.SpecFlow.Bindings.Reflection;

namespace SpecFlowConnector.SpecFlowProxies;

public record BindingMethodAdapter(IBindingMethod Adaptee)
{
    public IEnumerable<string> ParameterTypeNames => Adaptee.Parameters.Select(p => p.Type.FullName);
    public Option<MethodInfo> MethodInfo => (Adaptee as RuntimeBindingMethod)?.MethodInfo;
    public override string? ToString() => Adaptee.ToString();
}
