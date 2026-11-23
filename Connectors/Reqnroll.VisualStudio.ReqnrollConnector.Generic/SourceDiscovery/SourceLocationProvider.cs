using System.Reflection;
using System.Runtime.Loader;
using Reqnroll.Bindings.Provider.Data;
using ReqnrollConnector.Discovery;
using ReqnrollConnector.Logging;

namespace ReqnrollConnector.SourceDiscovery;

internal class SourceLocationProvider : ISourceLocationProvider, IBindingNavigationTargetProvider
{
    private readonly SymbolReaderCache _symbolReaders;
    private readonly AssemblyLoadContext _assemblyLoadContext;
    private readonly Assembly _testAssembly;
    private readonly ILogger _log;

    public SourceLocationProvider(AssemblyLoadContext assemblyLoadContext, Assembly testAssembly, ILogger log)
    {
        _symbolReaders = new SymbolReaderCache(log);
        _assemblyLoadContext = assemblyLoadContext;
        _testAssembly = testAssembly;
        _log = log;
    }

    public Reqnroll.VisualStudio.ReqnrollConnector.Models.BindingNavigationTarget? GetNavigationTarget(
        BindingSourceMethodData bindingMethod)
    {
        if (bindingMethod.MetadataToken == null)
            return null;
        try
        {
            var assembly = _assemblyLoadContext.LoadFromAssemblyName(
                new AssemblyName(bindingMethod.Assembly ?? _testAssembly.FullName!));
            var method = assembly.ManifestModule.ResolveMethod(bindingMethod.MetadataToken.Value) as MethodInfo;
            if (method?.DeclaringType?.FullName != bindingMethod.Type)
            {
                _log.Info($"Cannot determine navigation identity: metadata token does not match {bindingMethod.Type}.");
                return null;
            }
            return Reqnroll.VisualStudio.ReqnrollConnector.Models.BindingNavigationTarget.FromMethod(method);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is BadImageFormatException ||
                                   ex is FileNotFoundException || ex is FileLoadException || ex is TypeLoadException)
        {
            _log.Info($"Cannot determine navigation identity for {bindingMethod.FullName}: {ex.Message}");
            return null;
        }
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