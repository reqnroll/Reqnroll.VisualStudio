using System.Reflection;
using System.Runtime.Loader;
using Reqnroll.Bindings.Provider.Data;
using ReqnrollConnector.Discovery;
using ReqnrollConnector.Logging;

namespace ReqnrollConnector.SourceDiscovery;

internal class SourceLocationProvider : ISourceLocationProvider
{
    private readonly SymbolReaderCache _symbolReaders;
    private readonly AssemblyLoadContext _assemblyLoadContext;
    private readonly Assembly _testAssembly;

    /// <param name="testAssemblyPath">
    /// The original on-disk path of the test assembly. Required when the assembly
    /// was loaded via <c>LoadFromStream</c> and <c>Assembly.Location</c> is empty.
    /// </param>
    public SourceLocationProvider(
        AssemblyLoadContext assemblyLoadContext,
        Assembly testAssembly,
        string? testAssemblyPath,
        ILogger log)
    {
        _assemblyLoadContext = assemblyLoadContext;
        _testAssembly = testAssembly;

        // When the test assembly was loaded via LoadFromStream its Location is "".
        // Supply a resolver so SymbolReaderCache can still locate the file on disk.
        Func<Assembly, string?>? pathResolver = string.IsNullOrEmpty(testAssemblyPath)
            ? null
            : a => (a == _testAssembly && string.IsNullOrEmpty(a.Location))
                ? testAssemblyPath
                : (string.IsNullOrEmpty(a.Location) ? null : a.Location);

        _symbolReaders = new SymbolReaderCache(log, pathResolver);
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

        if (startSequencePoint != null && endSequencePoint != null)
        {
            return new SourceLocation(
                startSequencePoint.SourcePath,
                startSequencePoint.StartLine,
                startSequencePoint.StartColumn,
                endSequencePoint.EndLine,
                endSequencePoint.EndColumn);
        }

        return null;
    }
}