namespace Reqnroll.VisualStudio.Editor.Commands;

[Export(typeof(IDeveroomFeatureEditorCommand))]
public class UncommentCommand : DeveroomEditorCommandBase, IDeveroomFeatureEditorCommand
{
    [ImportingConstructor]
    public UncommentCommand(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider)
        : base(ideScope, aggregatorFactory, taggerProvider)
    {
    }

    public override DeveroomEditorCommandTargetKey[] Targets => new[]
    {
        new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK),
        new DeveroomEditorCommandTargetKey(VSConstants.VSStd2K, VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK)
    };

    public override bool PreExec(IWpfTextView textView, DeveroomEditorCommandTargetKey commandKey,
        IntPtr inArgs = default)
    {
        MonitoringService.MonitorCommandCommentUncomment();

        var selectionSpan = GetSelectionSpan(textView);
        var lines = GetSpanFullLines(selectionSpan).ToArray();
        Debug.Assert(lines.Length > 0);

        using (IdeScope.CreateUndoContext("Uncomment lines"))
        using (var textEdit = selectionSpan.Snapshot.TextBuffer.CreateEdit())
        {
            foreach (var line in lines)
            {
                int commentCharPosition = line.GetText().IndexOf('#');
                if (commentCharPosition >= 0)
                    textEdit.Delete(line.Start.Position + commentCharPosition, 1);
            }

            textEdit.Apply();
        }

        SetSelectionToChangedLines(textView, lines);

        return true;
    }
}
