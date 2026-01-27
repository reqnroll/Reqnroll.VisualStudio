using Reqnroll.Bindings.Provider.Data;

namespace ReqnrollConnector.Discovery;

public class SourceLocation
{
    public string SourcePath { get; }
    public int StartLine { get; }
    public int StartColumn { get; }
    public int EndLine { get; }
    public int EndColumn { get; }

    public SourceLocation(string sourcePath, int startLine, int startColumn, int endLine, int endColumn)
    {
        SourcePath = sourcePath;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }
}

public interface ISourceLocationProvider
{
    SourceLocation? GetSourceLocation(BindingSourceMethodData bindingMethod);
}

// ReSharper disable once UnusedMember.Global
public class NullSourceLocationProvider : ISourceLocationProvider
{
    public SourceLocation? GetSourceLocation(BindingSourceMethodData bindingMethod) => null;
}
