namespace SpecFlow.VisualStudio.SpecFlowConnector.SourceDiscovery;

public class SequencePoint
{
    public const int HiddenLine = 16707566;

    public SequencePoint(int ilOffset, string sourcePath, int startLine, int endLine, int startColumn, int endColumn)
    {
        IlOffset = ilOffset;
        SourcePath = sourcePath;
        StartLine = startLine;
        EndLine = endLine;
        StartColumn = startColumn;
        EndColumn = endColumn;
    }

    public bool IsHidden => StartLine == HiddenLine;

    public int IlOffset { get; }

    public string SourcePath { get; }

    public int StartLine { get; }

    public int EndLine { get; }

    public int StartColumn { get; }

    public int EndColumn { get; }
}
