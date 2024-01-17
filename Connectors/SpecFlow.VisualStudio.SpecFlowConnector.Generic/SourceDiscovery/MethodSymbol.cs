namespace SpecFlowConnector.SourceDiscovery;

public record MethodSymbolSequencePoint(
    int IlOffset,
    string SourcePath,
    int StartLine,
    int EndLine,
    int StartColumn,
    int EndColumn)
{
}
