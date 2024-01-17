using System;

namespace SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery;

public class MethodSymbol
{
    public MethodSymbol(int token, SequencePoint[] sequencePoints)
    {
        Token = token;
        SequencePoints = sequencePoints;
    }

    public int Token { get; }
    public SequencePoint[] SequencePoints { get; }
}

public interface IDeveroomSymbolReader : IDisposable
{
    MethodSymbol ReadMethodSymbol(int token);
}
