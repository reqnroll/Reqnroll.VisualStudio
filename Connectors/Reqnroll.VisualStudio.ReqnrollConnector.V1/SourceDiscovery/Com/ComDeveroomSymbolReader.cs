#nullable disable
using System.Diagnostics.SymbolStore;

namespace Reqnroll.VisualStudio.ReqnrollConnector.SourceDiscovery.Com;

public class ComDeveroomSymbolReader : IDeveroomSymbolReader
{
    private ISymbolReader _reader;

    public ComDeveroomSymbolReader(string assemblyPath)
    {
        _reader = SymUtil.GetSymbolReaderForFile(assemblyPath);
        if (_reader == null)
            throw new InvalidOperationException("Error: No matching PDB could be found for the specified assembly.");
    }

    public MethodSymbol ReadMethodSymbol(int token)
    {
        var symbolMethod = _reader.GetMethod(new SymbolToken(token));
        if (symbolMethod == null)
            return null;

        return new MethodSymbol(token, ReadSequencePoints(symbolMethod));
    }

    public void Dispose()
    {
        if (_reader is IDisposable disposableReader)
            disposableReader.Dispose();
        _reader = null;
    }

    // Write the sequence points for the given method
    // Sequence points are the map between IL offsets and source lines.
    // A single method could span multiple files (use C#'s #line directive to see for yourself).        
    private SequencePoint[] ReadSequencePoints(ISymbolMethod method)
    {
        int count = method.SequencePointCount;

        // Get the sequence points from the symbol store. 
        // We could cache these arrays and reuse them.
        int[] offsets = new int[count];
        ISymbolDocument[] docs = new ISymbolDocument[count];
        int[] startColumn = new int[count];
        int[] endColumn = new int[count];
        int[] startRow = new int[count];
        int[] endRow = new int[count];
        method.GetSequencePoints(offsets, docs, startRow, startColumn, endRow, endColumn);

        // Store them into the list
        var sequencePoints = new List<SequencePoint>(count);
        for (int i = 0; i < count; i++)
        {
            var sp = new SequencePoint(offsets[i], docs[i].URL, startRow[i], endRow[i], startColumn[i], endColumn[i]);
            sequencePoints.Add(sp);
        }

        return sequencePoints.OrderBy(sp => sp.SourcePath).ThenBy(sp => sp.StartLine).ToArray();
    }
}
