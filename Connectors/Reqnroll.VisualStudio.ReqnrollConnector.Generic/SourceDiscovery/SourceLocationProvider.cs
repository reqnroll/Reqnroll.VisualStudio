using System.Reflection;
using Reqnroll.Bindings.Provider.Data;
using ReqnrollConnector.AssemblyLoading;
using ReqnrollConnector.Discovery;
using ReqnrollConnector.Logging;

namespace ReqnrollConnector.SourceDiscovery;

internal class SourceLocationProvider : ISourceLocationProvider
{
    private readonly SymbolReaderCache _symbolReaders;
    private readonly ITestAssemblyContext _assemblyLoadContext;

    public SourceLocationProvider(ITestAssemblyContext assemblyLoadContext, ILogger log)
    {
        _symbolReaders = new SymbolReaderCache(log);
        _assemblyLoadContext = assemblyLoadContext;
    }

    public SourceLocation? GetSourceLocation(BindingSourceMethodData bindingMethod)
    {
        if (bindingMethod.MetadataToken == null)
            return null;

        var assemblyNameStr = bindingMethod.Assembly ?? _assemblyLoadContext.TestAssemblyFullName!;
        var assemblyLocation = _assemblyLoadContext.AssemblyLocationFromAssemblyName(assemblyNameStr);
        var reader = _symbolReaders[assemblyLocation];

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