#nullable disable
namespace SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery;

internal class NullDeveroomSymbolReader : IDeveroomSymbolReader
{
    public void Dispose()
    {
        //nop
    }

    public MethodSymbol ReadMethodSymbol(int token) => null;
}
