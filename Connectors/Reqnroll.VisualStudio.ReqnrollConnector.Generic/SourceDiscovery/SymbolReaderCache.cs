using System.Reflection;
using ReqnrollConnector.Logging;
using ReqnrollConnector.SourceDiscovery.DnLib;

namespace ReqnrollConnector.SourceDiscovery;

public class SymbolReaderCache
{
    private readonly ILogger _log;
    private readonly Dictionary<string, DeveroomSymbolReader?> _symbolReaders = new(2);

    public SymbolReaderCache(ILogger log)
    {
        _log = log;
    }

    public DeveroomSymbolReader? this[string assemblyLocation] => GetOrCreateSymbolReader(assemblyLocation);

    private DeveroomSymbolReader? GetOrCreateSymbolReader(string assemblyLocation)
    {
        if (_symbolReaders.TryGetValue(assemblyLocation, out var symbolReader))
            return symbolReader;

        var primaryReader = CreateSymbolReader(assemblyLocation);
        if (primaryReader != null)
        {
            _symbolReaders.Add(assemblyLocation, primaryReader);
            return primaryReader;
        }

        var secondaryReader = CreateSymbolReader(new Uri(assemblyLocation).LocalPath);
        if (secondaryReader != null)
        {
            _symbolReaders.Add(assemblyLocation, secondaryReader);
            return secondaryReader;
        }

        _symbolReaders.Add(assemblyLocation, null);
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
