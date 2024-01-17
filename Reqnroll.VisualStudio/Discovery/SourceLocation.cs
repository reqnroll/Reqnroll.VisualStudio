#nullable disable

public class SourceLocation
{
    public SourceLocation(string sourceFile, int sourceFileLine, int sourceFileColumn, int? sourceFileEndLine = null,
        int? sourceFileEndColumn = null)
    {
        SourceFile = sourceFile;
        SourceFileLine = sourceFileLine;
        SourceFileColumn = sourceFileColumn;
        SourceFileEndLine = sourceFileEndLine;
        SourceFileEndColumn = sourceFileEndColumn;
    }

    public string SourceFile { get; }
    public int SourceFileLine { get; } // 1-based
    public int SourceFileColumn { get; } // 1-based
    public int? SourceFileEndLine { get; } // 1-based
    public int? SourceFileEndColumn { get; } // 1-based

    public IPersistentSpan SourceLocationSpan { get; set; }

    public bool HasEndPosition => SourceFileEndLine != null && SourceFileEndColumn != null;

    public override string ToString() => $"{SourceFile}({SourceFileLine},{SourceFileColumn})";
}
