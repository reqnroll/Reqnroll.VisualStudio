#nullable disable
namespace Reqnroll.VisualStudio.Editor.Commands.Infrastructure;

public abstract class DeveroomEditorCommandBase : IDeveroomEditorCommand
{
    protected readonly IBufferTagAggregatorFactoryService AggregatorFactory;
    protected readonly IIdeScope IdeScope;

    protected DeveroomEditorCommandBase(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider)
    {
        IdeScope = ideScope;
        AggregatorFactory = aggregatorFactory;
        DeveroomTaggerProvider = taggerProvider;
    }

    protected IDeveroomTaggerProvider DeveroomTaggerProvider { get; }

    protected IMonitoringService MonitoringService => IdeScope.MonitoringService;

    public AsyncManualResetEvent Finished { get; } = new();

    public virtual DeveroomEditorCommandTargetKey Target
        => throw new NotImplementedException();

    protected IDeveroomLogger Logger => IdeScope.Logger;

    public virtual DeveroomEditorCommandTargetKey[] Targets
        => new[] {Target};

    public virtual DeveroomEditorCommandStatus QueryStatus(IWpfTextView textView,
        DeveroomEditorCommandTargetKey commandKey) =>
        DeveroomEditorCommandStatus.Supported;

    public void Prepare()
    {
        Finished.Reset();
    }

    public virtual bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default) =>
        false;

    public virtual bool PostExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default) =>
        false;

    protected DeveroomTag GetDeveroomTagForCaret(IWpfTextView textView, params string[] tagTypes)
    {
        var tagger = DeveroomTaggerProvider.CreateTagger<DeveroomTag>(textView.TextBuffer);
        var caretSpan = new SnapshotSpan(textView.Caret.Position.BufferPosition, 0);
        var tags = tagger.GetUpToDateDeveroomTagsForSpan(caretSpan);

        var tag = DumpDeveroomTags(tags)
            .Where(t => tagTypes.Contains(t.Tag.Type))
            .Select(t => t.Tag)
            .DefaultIfEmpty(VoidDeveroomTag.Instance)
            .First();

        return tag;
    }

    protected IEnumerable<ITagSpan<DeveroomTag>> DumpDeveroomTags(IEnumerable<ITagSpan<DeveroomTag>> deveroomTags)
    {
#if DEBUG
        foreach (var deveroomTag in deveroomTags)
        {
            Logger.LogVerbose($"  Tag: {deveroomTag.Tag.Type} @ {deveroomTag.Span}");
            yield return deveroomTag;
        }
#else
            return deveroomTags;
#endif
    }

    protected string GetEditorDocumentPath(ITextBuffer textBuffer)
    {
        if (!textBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter))
            return null;

        if (!(bufferAdapter is IPersistFileFormat persistFileFormat))
            return null;

        if (!ErrorHandler.Succeeded(persistFileFormat.GetCurFile(out string filePath, out _)))
            return null;

        return filePath;
    }

    protected DeveroomConfiguration GetConfiguration(IWpfTextView textView)
    {
        var configuration = IdeScope.GetDeveroomConfiguration(IdeScope.GetProject(textView.TextBuffer));
        return configuration;
    }

    #region Helper methods

    protected void SetSelectionToChangedLines(IWpfTextView textView, ITextSnapshotLine[] lines)
    {
        var newSnapshot = textView.TextBuffer.CurrentSnapshot;
        var selectionStartPosition = newSnapshot.GetLineFromLineNumber(lines.First().LineNumber).Start;
        var selectionEndPosition = newSnapshot.GetLineFromLineNumber(lines.Last().LineNumber).End;
        textView.Selection.Select(new SnapshotSpan(
            selectionStartPosition,
            selectionEndPosition), false);
        textView.Caret.MoveTo(selectionEndPosition);
    }

    protected SnapshotSpan GetSelectionSpan(IWpfTextView textView) =>
        new(textView.Selection.Start.Position, textView.Selection.End.Position);

    protected IEnumerable<ITextSnapshotLine> GetSpanFullLines(SnapshotSpan span)
    {
        var selectionStartLine = span.Start.GetContainingLine();
        var selectionEndLine = GetSelectionEndLine(selectionStartLine, span);
        return GetSpanFullLines(selectionStartLine.Snapshot, selectionStartLine.LineNumber,
            selectionEndLine.LineNumber);
    }

    internal static IEnumerable<ITextSnapshotLine> GetSpanFullLines(ITextSnapshot textSnapshot, int startLine,
        int endLine)
    {
        for (int lineNumber = startLine; lineNumber <= endLine; lineNumber++)
            yield return textSnapshot.GetLineFromLineNumber(lineNumber);
    }

    protected IEnumerable<ITextSnapshotLine> GetSpanFullLines(ITextSnapshot textSnapshot)
    {
        for (int lineNumber = 0; lineNumber < textSnapshot.LineCount; lineNumber++)
            yield return textSnapshot.GetLineFromLineNumber(lineNumber);
    }

    private ITextSnapshotLine GetSelectionEndLine(ITextSnapshotLine selectionStartLine, SnapshotSpan span)
    {
        var selectionEndLine = span.End.GetContainingLine();
        // if the selection ends exactly at the beginning of a new line (ie line select), we do not comment out the last line
        if (selectionStartLine.LineNumber != selectionEndLine.LineNumber && selectionEndLine.Start.Equals(span.End))
            selectionEndLine = selectionEndLine.Snapshot.GetLineFromLineNumber(selectionEndLine.LineNumber - 1);
        return selectionEndLine;
    }

    protected string GetNewLine(IWpfTextView textView)
    {
        // based on EditorOperations.InsertNewLine()
        if (textView.Options.GetReplicateNewLineCharacter())
        {
            var caretLine = textView.Caret.Position.BufferPosition.GetContainingLine();
            if (caretLine.LineBreakLength > 0)
                return caretLine.GetLineBreakText();
            if (textView.TextSnapshot.LineCount > 1)
                return textView.TextSnapshot.GetLineFromLineNumber(textView.TextSnapshot.LineCount - 2)
                    .GetLineBreakText();
        }

        return textView.Options.GetNewLineCharacter();
    }

    #endregion
}
