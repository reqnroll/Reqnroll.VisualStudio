namespace Reqnroll.VisualStudio.Editor.Commands;

[Export(typeof(IDeveroomFeatureEditorCommand))]
public class CommentCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
{
    [ImportingConstructor]
    public CommentCommand(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider)
        : base(ideScope, aggregatorFactory, taggerProvider)
    {
    }

    public override DeveroomEditorCommandTargetKey[] Targets => new[]
    {
        new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.COMMENTBLOCK),
        new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.COMMENT_BLOCK)
    };

    public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default)
    {
        MonitoringService.MonitorCommandCommentUncomment();

        var selectionSpan = GetSelectionSpan(textView);
        var lines = GetSpanFullLines(selectionSpan).ToArray();
        Debug.Assert(lines.Length > 0);

        int indent = lines.Min(l => l.GetText().TakeWhile(char.IsWhiteSpace).Count());

        using (IdeScope.CreateUndoContext("Comment lines"))
        using (var textEdit = selectionSpan.Snapshot.TextBuffer.CreateEdit())
        {
            foreach (var line in lines) textEdit.Insert(line.Start.Position + indent, "#");
            textEdit.Apply();
        }

        SetSelectionToChangedLines(textView, lines);

        return true;
    }
}
