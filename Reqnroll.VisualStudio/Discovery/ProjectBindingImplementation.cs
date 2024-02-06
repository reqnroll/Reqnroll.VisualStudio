namespace Reqnroll.VisualStudio.Discovery;

public class ProjectBindingImplementation
{
    private static readonly string[] EmptyParameterTypes = Array.Empty<string>();

    public ProjectBindingImplementation(string method, string[]? parameterTypes, SourceLocation sourceLocation)
    {
        Method = method;
        ParameterTypes = parameterTypes ?? EmptyParameterTypes;
        SourceLocation = sourceLocation;
    }

    public string Method { get; } //TODO: Name, URI, SourceType?
    public string[] ParameterTypes { get; }
    public SourceLocation? SourceLocation { get; }

    public override string ToString() => Method;
}
