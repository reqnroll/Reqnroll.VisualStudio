namespace Reqnroll.VisualStudio.Editor.Completions;

[Export(typeof(IDeveroomFeatureEditorCommand))]
public class CompleteCommand : CompletionCommandBase, IDeveroomFeatureEditorCommand
{
    [ImportingConstructor]
    public CompleteCommand(
        IIdeScope ideScope,
        IBufferTagAggregatorFactoryService aggregatorFactory,
        IDeveroomTaggerProvider taggerProvider,
        ICompletionBroker completionBroker)
        : base(ideScope, aggregatorFactory, taggerProvider, completionBroker)
    {
    }

    protected override bool ShouldStartSessionOnTyping(IWpfTextView textView, char? ch, bool isSessionActive)
    {
        var caretBufferPosition = textView.Caret.Position.BufferPosition;
        var line = caretBufferPosition.GetContainingLine();

        if (ch == null || char.IsWhiteSpace(ch.Value))
        {
            var showStepCompletionAfterStepKeywords =
                GetConfiguration(textView).Editor.ShowStepCompletionAfterStepKeywords;
            var containingText = line.GetText();

            if (showStepCompletionAfterStepKeywords &&
                Regex.IsMatch(containingText, "^\\s*\\S+\\s$") &&
                GetDeveroomTagForCaret(textView, DeveroomTagTypes.StepKeyword) != VoidDeveroomTag.Instance)
                return true;
        }

        if (ch == null || ch == '|' || ch == '#' || ch == '*' || ch == '@' || isSessionActive)
            return false;

        if (caretBufferPosition == line.Start)
            return false; // we are at the beginning of a line (after an enter?)

        var linePrefixText = new SnapshotSpan(line.Start, caretBufferPosition.Subtract(1)).GetText();
        return
            linePrefixText.All(char
                .IsWhiteSpace); // start auto completion for the first typed in character in the line 
    }
}
