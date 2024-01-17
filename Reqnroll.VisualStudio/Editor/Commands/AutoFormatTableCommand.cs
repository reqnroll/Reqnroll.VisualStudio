#nullable disable
namespace Reqnroll.VisualStudio.Editor.Commands;

[Export(typeof(IDeveroomFeatureEditorCommand))]
public class AutoFormatTableCommand : DeveroomEditorTypeCharCommandBase, IDeveroomFeatureEditorCommand
{
    private readonly IEditorConfigOptionsProvider _editorConfigOptionsProvider;

    private readonly GherkinDocumentFormatter _gherkinDocumentFormatter;

    [ImportingConstructor]
    public AutoFormatTableCommand(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider,
        GherkinDocumentFormatter gherkinDocumentFormatter,
        IEditorConfigOptionsProvider editorConfigOptionsProvider)
        : base(ideScope, aggregatorFactory, taggerProvider)
    {
        _gherkinDocumentFormatter = gherkinDocumentFormatter;
        _editorConfigOptionsProvider = editorConfigOptionsProvider;
    }

    protected internal override bool PostExec(IWpfTextView textView, char ch)
    {
        if (ch != '|')
            return false;

        var dataTableTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.DataTable, DeveroomTagTypes.ExamplesBlock);
        if (dataTableTag.Data is not IHasRows hasRows)
            return false;

        var currentText = dataTableTag.Span.GetText();
        if (IsEscapedPipeTyped(textView.Caret.Position.BufferPosition))
            return false;

        var lines = new DocumentLinesEditBuffer(dataTableTag.Span);
        if (lines.IsEmpty)
            return false;

        var indent = GetIndent(textView.TextSnapshot, hasRows);
        var newLine = GetNewLine(textView);
        var formatSettings =
            GherkinFormatSettings.Load(_editorConfigOptionsProvider, textView, GetConfiguration(textView));

        var caretPosition = GetCaretPosition(textView.Caret.Position, hasRows);
        var widths = _gherkinDocumentFormatter.GetTableWidths(hasRows);
        _gherkinDocumentFormatter.FormatTable(lines, hasRows, formatSettings, indent, widths);

        var formattedTableText = lines.GetModifiedText(newLine);

        var newCaretLineColumn = CalculateNewCaretLinePosition(caretPosition, widths, indent, formatSettings);
        if (currentText.Equals(formattedTableText))
            return false;

        MonitoringService.MonitorCommandAutoFormatTable();

        using (IdeScope.CreateUndoContext("Auto format table"))
        using (var textEdit = textView.TextBuffer.CreateEdit())
        {
            textEdit.Replace(dataTableTag.Span, formattedTableText);
            textEdit.Apply();
        }

        RestoreCaretPosition(textView, caretPosition, newCaretLineColumn);
        return true;
    }

    private bool IsEscapedPipeTyped(SnapshotPoint positionBufferPosition)
    {
        positionBufferPosition = positionBufferPosition.Subtract(1);
        int backslashCount = 0;
        while (positionBufferPosition.Position > 0)
        {
            positionBufferPosition = positionBufferPosition.Subtract(1);
            if (positionBufferPosition.GetChar() != '\\')
                break;
            backslashCount++;
        }

        return backslashCount % 2 == 1;
    }

    private string GetIndent(ITextSnapshot textSnapshot, IHasRows hasRows)
    {
        var firstLine = textSnapshot.GetLineFromLineNumber(hasRows.Rows.First().Location.Line - 1);
        return Regex.Match(firstLine.GetText(), @"^\s*").Value;
    }

    private void RestoreCaretPosition(IWpfTextView textView, TableCaretPosition caretPosition, int newCaretLineColumn)
    {
        var line = textView.TextSnapshot.GetLineFromLineNumber(caretPosition.Line);
        var lineStartPosition = line.Start.Position;
        textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot,
            Math.Min(lineStartPosition + newCaretLineColumn, line.End.Position)));
    }

    private TableCaretPosition GetCaretPosition(CaretPosition caretPosition, IHasRows hasRows)
    {
        var line = caretPosition.BufferPosition.GetContainingLine();
        var caretColumn = caretPosition.BufferPosition.Position - line.Start.Position;
        var row = hasRows.Rows.FirstOrDefault(r => r.Location.Line == line.LineNumber + 1);
        if (row == null || row.Cells.Any(c => c.Location.Column <= 0))
            return TableCaretPosition.CreateUnknown(line.LineNumber, caretColumn);

        var cellIndex = FindCellIndex(caretColumn, row, line.GetText(), out var cell);
        if (cellIndex < 0) // before first cell
            return TableCaretPosition.CreateBeforeFirstCell(line.LineNumber, caretColumn);
        if (cellIndex == int.MaxValue) // after last cell
            return TableCaretPosition.CreateAfterLastCell(line.LineNumber);

        return TableCaretPosition.CreateInCell(line.LineNumber, cellIndex,
            Math.Max(caretColumn - (cell.Location.Column - 1), 0));
    }

    private int FindCellIndex(int caretColumn, TableRow row, string lineText, out TableCell cell)
    {
        cell = null;
        int cellIndex = -1;
        int lastCellStart = 0;
        foreach (var tableCell in row.Cells)
        {
            if (tableCell.Location.Column - 1 > caretColumn)
            {
                // either the previous cell or the padding of this cell
                if (!ContainsPipe(lineText.Substring(lastCellStart, caretColumn - lastCellStart)))
                    return cellIndex; // previous cell

                cell = tableCell;
                return cellIndex + 1;
            }

            cell = tableCell;
            lastCellStart = tableCell.Location.Column - 1;
            cellIndex++;
        }

        // either the last cell or after last
        if (!ContainsPipe(lineText.Substring(lastCellStart, caretColumn - lastCellStart)))
            return cellIndex; // last cell

        cell = null;
        return int.MaxValue;
    }

    private bool ContainsPipe(string s) => FindPipeIndex(s) >= 0;

    private int FindPipeIndex(string s)
    {
        var pipeMatch = Regex.Match(s, @"(?<![^\\](\\\\)*\\)\|");
        return pipeMatch.Success ? pipeMatch.Index : -1;
    }

    private int CalculateNewCaretLinePosition(TableCaretPosition caretPosition, int[] widths, string indent,
        GherkinFormatSettings formatSettings)
    {
        const int PIPE_LENGTH = 1;
        if (caretPosition.IsUnknown) return caretPosition.Column;
        if (caretPosition.IsBeforeFirstCell) return Math.Min(caretPosition.Column, indent.Length);

        Debug.Assert(caretPosition.Cell != null);
        var positionAfterCellOpenPipe =
            indent.Length +
            widths.Take(Math.Min(widths.Length, caretPosition.Cell!.Value))
                .Sum(w => w + formatSettings.TableCellPadding.Length * 2 + PIPE_LENGTH) +
            PIPE_LENGTH;

        if (caretPosition.IsAfterLastCell)
            return
                positionAfterCellOpenPipe; // position after the last cell pipe (position of the open pipe of the one after last cell)

        Debug.Assert(caretPosition.IsInCell);
        // position within the cell + 2*padding, if cell has been shrunk, we move position to end of cell (Math.Min)
        return positionAfterCellOpenPipe +
               formatSettings.TableCellPadding.Length +
               Math.Min(caretPosition.Column,
                   widths[caretPosition.Cell.Value] + formatSettings.TableCellPadding.Length);
    }

    private readonly struct TableCaretPosition
    {
        public readonly int Line;
        public readonly int Column;
        public readonly int? Cell;

        public bool IsUnknown => Cell == null;
        public bool IsBeforeFirstCell => Cell == null;
        public bool IsAfterLastCell => Cell == int.MaxValue;
        public bool IsInCell => !IsUnknown && !IsBeforeFirstCell && !IsAfterLastCell;

        private TableCaretPosition(int line, int column, int? cell)
        {
            Line = line;
            Column = column;
            Cell = cell;
        }

        public static TableCaretPosition CreateUnknown(int line, int column) => new(line, column, null);
        public static TableCaretPosition CreateBeforeFirstCell(int line, int column) => new(line, column, null);

        public static TableCaretPosition CreateInCell(int line, int cell, int withinCellColumn) =>
            new(line, withinCellColumn, cell);

        public static TableCaretPosition CreateAfterLastCell(int line) => new(line, 0, int.MaxValue);
    }
}
