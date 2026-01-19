using ReqnrollConnector.SourceDiscovery.DnLib;

namespace ReqnrollConnector.SourceDiscovery;

public class SymbolReaderCache
{
    private readonly ILogger _log;
    private readonly Dictionary<Assembly, Option<DeveroomSymbolReader>> _symbolReaders = new(2);

    public SymbolReaderCache(ILogger log)
    {
        _log = log;
    }

    public Option<DeveroomSymbolReader> this[Assembly assembly] => GetOrCreateSymbolReader(assembly);

    private Option<DeveroomSymbolReader> GetOrCreateSymbolReader(Assembly assembly)
    {
        if (_symbolReaders.TryGetValue(assembly, out var symbolReader))
            return symbolReader;

        var primaryReaderOption = CreateSymbolReader(assembly.Location);
        if (primaryReaderOption is Some<DeveroomSymbolReader>)
        {
            var reader = ((Some<DeveroomSymbolReader>)primaryReaderOption).Content;
            _symbolReaders.Add(assembly, reader);
            return reader;
        }

        var secondaryReaderOption = CreateSymbolReader(new Uri(assembly.Location).LocalPath);
        if (secondaryReaderOption is Some<DeveroomSymbolReader>)
        {
            var reader = ((Some<DeveroomSymbolReader>)secondaryReaderOption).Content;
            _symbolReaders.Add(assembly, reader);
            return reader;
        }

        var noneValue = None<DeveroomSymbolReader>.Value;
        _symbolReaders.Add(assembly, noneValue);
        return noneValue;
    }

    protected Option<DeveroomSymbolReader> CreateSymbolReader(
        string assemblyFilePath)
    {
        var factories = SymbolReaderFactories(assemblyFilePath);
        var readers = factories.SelectOptional(TryCreateReader);
        var firstReader = readers.FirstOrNone();
        return firstReader;
    }

    private IEnumerable<Func<DeveroomSymbolReader>> SymbolReaderFactories(string path)
    {
        return new Func<DeveroomSymbolReader> []
        {
            () => DnLibDeveroomSymbolReader.Create(_log, path)
        };
    }

    private Option<DeveroomSymbolReader> TryCreateReader(Func<DeveroomSymbolReader> factory)
    {
        try
        {
            return factory();
        }
        catch (Exception ex)
        {
            _log.Error(ex.ToString());
        }

        return None<DeveroomSymbolReader>.Value;
    }
}
