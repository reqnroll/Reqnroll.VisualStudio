using ReqnrollConnector.SourceDiscovery.DnLib;

namespace ReqnrollConnector.SourceDiscovery;

public class SymbolReaderCache
{
    private readonly ILogger _log;
    private readonly Dictionary<Assembly, DeveroomSymbolReader?> _symbolReaders = new(2);

    public SymbolReaderCache(ILogger log)
    {
        _log = log;
    }

    public DeveroomSymbolReader? this[Assembly assembly] => GetOrCreateSymbolReader(assembly);

    private DeveroomSymbolReader? GetOrCreateSymbolReader(Assembly assembly)
    {
        if (_symbolReaders.TryGetValue(assembly, out var symbolReader))
            return symbolReader;

        var primaryReader = CreateSymbolReader(assembly.Location);
        if (primaryReader != null)
        {
            _symbolReaders.Add(assembly, primaryReader);
            return primaryReader;
        }

        var secondaryReader = CreateSymbolReader(new Uri(assembly.Location).LocalPath);
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
            {
                return reader;
            }
        }
        
        return null;
    }

    private IEnumerable<Func<DeveroomSymbolReader>> SymbolReaderFactories(string path)
    {
        return new Func<DeveroomSymbolReader> []
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
