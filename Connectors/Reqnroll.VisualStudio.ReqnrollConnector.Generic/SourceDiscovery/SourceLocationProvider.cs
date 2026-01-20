using Reqnroll.Bindings.Provider.Data;

namespace ReqnrollConnector.SourceDiscovery;

internal class SourceLocationProvider : ISourceLocationProvider
{
    private readonly SymbolReaderCache _symbolReaders;
    private readonly AssemblyLoadContext _assemblyLoadContext;
    private readonly Assembly _testAssembly;

    public SourceLocationProvider(AssemblyLoadContext assemblyLoadContext, Assembly testAssembly, ILogger log)
    {
        _symbolReaders = new SymbolReaderCache(log);
        _assemblyLoadContext = assemblyLoadContext;
        _testAssembly = testAssembly;
    }

    public SourceLocation? GetSourceLocation(BindingSourceMethodData bindingMethod)
    {
        if (bindingMethod.MetadataToken == null)
            return null;

        var assemblyNameStr = bindingMethod.Assembly ?? _testAssembly.FullName!;
        var assemblyNameObj = new AssemblyName(assemblyNameStr);
        var assembly = _assemblyLoadContext.LoadFromAssemblyName(assemblyNameObj);
        var reader = _symbolReaders[assembly];

        if (reader == null)
            return null;

        var sequencePoints = reader.ReadMethodSymbol(bindingMethod.MetadataToken.Value);

        // Find start and end sequence points
        var (startSequencePoint, endSequencePoint) = sequencePoints.Aggregate(
            (startSequencePoint: (MethodSymbolSequencePoint?)null,
                endSequencePoint: (MethodSymbolSequencePoint?)null),
            (acc, cur) =>
            {
                if (acc.startSequencePoint == null)
                    return (cur, cur);
                return (acc.startSequencePoint, cur);
            }
        );

        // Extract the points
        if (startSequencePoint != null && endSequencePoint != null)
        {
            return new SourceLocation(startSequencePoint.SourcePath, startSequencePoint.StartLine, startSequencePoint.StartColumn, endSequencePoint.EndLine, endSequencePoint.EndColumn);
        }

        return null;
    }
}