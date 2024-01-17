namespace Reqnroll.VisualStudio.Editor.Commands;

[Export(typeof(IDeveroomFeatureEditorCommand))]
public class AutoFormatDocumentCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
{
    internal static readonly DeveroomEditorCommandTargetKey FormatDocumentKey =
        new(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.FORMATDOCUMENT);

    internal static readonly DeveroomEditorCommandTargetKey FormatSelectionKey =
        new(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.FORMATSELECTION);

    private readonly IEditorConfigOptionsProvider _editorConfigOptionsProvider;

    private readonly GherkinDocumentFormatter _gherkinDocumentFormatter;

    [ImportingConstructor]
    public AutoFormatDocumentCommand(
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

    public override DeveroomEditorCommandTargetKey[] Targets => new[]
    {
        FormatDocumentKey,
        FormatSelectionKey
    };

    public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default)
    {
        var documentTag = GetDeveroomTagForCaret(textView, DeveroomTagTypes.Document);
        if (documentTag.Data is not DeveroomGherkinDocument gherkinDocument)
            return false;

        var isSelectionFormatting = commandKey.Equals(FormatSelectionKey);
        MonitoringService.MonitorCommandAutoFormatDocument(isSelectionFormatting);

        var textSnapshot = textView.TextSnapshot;
        var caretLineNumber = textView.Caret.Position.BufferPosition.GetContainingLine().LineNumber;

        var startLine = 0;
        var endLine = textSnapshot.LineCount - 1;

        if (isSelectionFormatting)
        {
            var selectionSpan = GetSelectionSpan(textView);
            startLine = selectionSpan.Start.GetContainingLine().LineNumber;
            endLine = selectionSpan.End.GetContainingLine().LineNumber;
        }

        var formatSettings =
            GherkinFormatSettings.Load(_editorConfigOptionsProvider, textView, GetConfiguration(textView));

        var lines = new DocumentLinesEditBuffer(textSnapshot, startLine, endLine);
        if (lines.IsEmpty)
            return false;

        _gherkinDocumentFormatter.FormatGherkinDocument(gherkinDocument, lines, formatSettings);
        var changeSpan = lines.GetSnapshotSpan();
        var newLine = GetNewLine(textView);
        var replacementText = lines.GetModifiedText(newLine);

        if (changeSpan.GetText().Equals(replacementText)) // no change
            return false;

        using (IdeScope.CreateUndoContext("Auto format document"))
        using (var textEdit = textSnapshot.TextBuffer.CreateEdit())
        {
            textEdit.Replace(changeSpan, replacementText);
            textEdit.Apply();
        }

        var lineFromLineNumber = textView.TextSnapshot.GetLineFromLineNumber(caretLineNumber);
        textView.Caret.MoveTo(lineFromLineNumber.End);
        textView.ViewScroller.EnsureSpanVisible(lineFromLineNumber.Extent);

        return true;
    }
}
