namespace Reqnroll.VisualStudio.Discovery;

public class ProjectStepDefinitionImplementation
{
    private static readonly string[] EmptyParameterTypes = new string[0];

    public ProjectStepDefinitionImplementation(string method, string[] parameterTypes, SourceLocation sourceLocation)
    {
        Method = method;
        ParameterTypes = parameterTypes ?? EmptyParameterTypes;
        SourceLocation = sourceLocation;
    }

    public string Method { get; } //TODO: Name, URI, SourceType?
    public string[] ParameterTypes { get; }
    public SourceLocation SourceLocation { get; }

    public override string ToString() => Method;
}
