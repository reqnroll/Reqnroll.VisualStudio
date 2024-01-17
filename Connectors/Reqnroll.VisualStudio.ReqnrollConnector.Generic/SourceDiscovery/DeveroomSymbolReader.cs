namespace ReqnrollConnector.SourceDiscovery;

public abstract class DeveroomSymbolReader
{
    public abstract IEnumerable<MethodSymbolSequencePoint> ReadMethodSymbol(int token);
}
