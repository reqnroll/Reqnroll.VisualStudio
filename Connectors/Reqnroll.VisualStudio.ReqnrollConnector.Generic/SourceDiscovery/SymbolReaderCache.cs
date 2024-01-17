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

        return CreateSymbolReader(assembly.Location)
            .Or(() => CreateSymbolReader(new Uri(assembly.Location).LocalPath))
            .Tie(reader => _symbolReaders.Add(assembly, reader));
    }

    protected Option<DeveroomSymbolReader> CreateSymbolReader(
        string assemblyFilePath) =>
        SymbolReaderFactories(assemblyFilePath)
            .SelectOptional(TryCreateReader)
            .FirstOrNone();

    private IEnumerable<Func<DeveroomSymbolReader>> SymbolReaderFactories(string path)
    {
        return new Func<DeveroomSymbolReader>[]
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
