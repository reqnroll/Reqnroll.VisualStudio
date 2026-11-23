namespace Reqnroll.VisualStudio.Discovery;

public class ProjectBindingImplementation
{
    private static readonly string[] EmptyParameterTypes = Array.Empty<string>();

    public ProjectBindingImplementation(string method, string[]? parameterTypes, SourceLocation? sourceLocation,
        ReqnrollConnector.Models.BindingNavigationTarget? navigationTarget = null)
    {
        Method = method;
        ParameterTypes = parameterTypes ?? EmptyParameterTypes;
        SourceLocation = sourceLocation;
        NavigationTarget = navigationTarget;
    }

    public string Method { get; } //TODO: Name, URI, SourceType?
    public string[] ParameterTypes { get; }
    public SourceLocation? SourceLocation { get; }
    public ReqnrollConnector.Models.BindingNavigationTarget? NavigationTarget { get; }

    public override string ToString() => Method;
}
