#nullable disable
namespace Reqnroll.VisualStudio.ReqnrollConnector.SourceDiscovery;

internal class NullDeveroomSymbolReader : IDeveroomSymbolReader
{
    public void Dispose()
    {
        //nop
    }

    public MethodSymbol ReadMethodSymbol(int token) => null;
}
