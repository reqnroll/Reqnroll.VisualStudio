using System.Reflection;
using ReqnrollConnector.Logging;
using ReqnrollConnector.SourceDiscovery.DnLib;

namespace ReqnrollConnector.SourceDiscovery;

public class SymbolReaderCache
{
    private readonly ILogger _log;
    private readonly Func<Assembly, string?> _pathResolver;
    private readonly Dictionary<Assembly, DeveroomSymbolReader?> _symbolReaders = new(2);

    /// <param name="pathResolver">
    /// Optional delegate that maps an <see cref="Assembly"/> to its on-disk path.
    /// Defaults to <c>Assembly.Location</c>. Supply a custom resolver when the
    /// assembly was loaded via <see cref="System.Runtime.Loader.AssemblyLoadContext.LoadFromStream"/>
    /// and <c>Assembly.Location</c> is therefore empty.
    /// </param>
    public SymbolReaderCache(ILogger log, Func<Assembly, string?>? pathResolver = null)
    {
        _log = log;
        _pathResolver = pathResolver ?? (a => string.IsNullOrEmpty(a.Location) ? null : a.Location);
    }

    public DeveroomSymbolReader? this[Assembly assembly] => GetOrCreateSymbolReader(assembly);

    private DeveroomSymbolReader? GetOrCreateSymbolReader(Assembly assembly)
    {
        if (_symbolReaders.TryGetValue(assembly, out var symbolReader))
            return symbolReader;

        var path = _pathResolver(assembly);
        if (string.IsNullOrEmpty(path))
        {
            _log.Info($"No on-disk path for {assembly.GetName().Name}; symbol reader unavailable.");
            _symbolReaders.Add(assembly, null);
            return null;
        }

        var primaryReader = CreateSymbolReader(path);
        if (primaryReader != null)
        {
            _symbolReaders.Add(assembly, primaryReader);
            return primaryReader;
        }

        var secondaryReader = CreateSymbolReader(new Uri(path).LocalPath);
        if (secondaryReader != null)
        {
            _symbolReaders.Add(assembly, secondaryReader);
            return secondaryReader;
        }

        _symbolReaders.Add(assembly, null);
        return null;
    }

    protected DeveroomSymbolReader? CreateSymbolReader(string assemblyFilePath)
    {
        var factories = SymbolReaderFactories(assemblyFilePath);
        var readerOptions = factories.Select(TryCreateReader).ToList();
        
        foreach (var reader in readerOptions)
        {
            if (reader != null)
                return reader;
        }
        
        return null;
    }

    private IEnumerable<Func<DeveroomSymbolReader>> SymbolReaderFactories(string path)
    {
        return new[]
        {
            () => DnLibDeveroomSymbolReader.Create(_log, path)
        };
    }

    private DeveroomSymbolReader? TryCreateReader(Func<DeveroomSymbolReader> factory)
    {
        try
        {
            return factory();
        }
        catch (Exception ex)
        {
            _log.Error(ex.ToString());
        }

        return null;
    }
}
