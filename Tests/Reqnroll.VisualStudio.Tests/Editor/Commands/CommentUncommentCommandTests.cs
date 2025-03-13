namespace Reqnroll.VisualStudio.Tests.Editor.Commands;

public class CommentUncommentCommandTests
{
    private readonly IIdeScope _ideScope;
    private readonly IDeveroomTaggerProvider _taggerProvider;

    public CommentUncommentCommandTests(ITestOutputHelper testOutputHelper)
    {
        _taggerProvider = Substitute.For<IDeveroomTaggerProvider>();
        _ideScope = new StubIdeScope(testOutputHelper);
    }

    [Theory]
    [InlineData("cared in first-mid line", 0, 2, null, null, @"#Feature: foo
Scenario: bar
")]
    [InlineData("caret at line end", 1, -1, null, null, @"Feature: foo
#Scenario: bar
")]
    [InlineData("caret in last empty line", 2, 0, null, null, @"Feature: foo
Scenario: bar
#")]
    [InlineData("two lines selected", 0, 0, 1, 2, @"#Feature: foo
#Scenario: bar
")]
    public void Can_comment_out_lines(string test, int selectionStartLine, int selectionStartColumn,
        int? selectionEndLine, int? selectionEndColumn, string expectedTextValue)
    {
        Console.WriteLine($"running: {test}");
        var command = new CommentCommand(
            _ideScope,
            new StubBufferTagAggregatorFactoryService(_taggerProvider),
            _taggerProvider);
        var inputText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "");
        var textView = CreateTextView(inputText, selectionStartLine, selectionStartColumn, selectionEndLine,
            selectionEndColumn);

        command.PreExec(textView, command.Targets.First());

        var expectedText = new TestText(expectedTextValue);
        Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());

        AssertLinesSelected(textView, expectedText, selectionStartLine, selectionEndLine);
    }

    [Theory]
    [InlineData("cared in first-mid line", 0, 2, null, null, @"#Feature: foo
Scenario: bar
")]
    [InlineData("caret at line end", 1, -1, null, null, @"Feature: foo
#Scenario: bar
")]
    [InlineData("caret in last empty line", 2, 0, null, null, @"Feature: foo
Scenario: bar
#")]
    [InlineData("two lines selected", 0, 0, 1, 2, @"#Feature: foo
#Scenario: bar
")]
    [InlineData("nothing to uncomment", 0, 2, null, null, @"Feature: foo
Scenario: bar
")]
    public void Can_uncomment_lines(string test, int selectionStartLine, int selectionStartColumn,
        int? selectionEndLine, int? selectionEndColumn, string inputTextValue)
    {
        Console.WriteLine($"running: {test}");
        var command = new UncommentCommand(
            _ideScope,
            new StubBufferTagAggregatorFactoryService(_taggerProvider),
            _taggerProvider);
        var inputText = new TestText(inputTextValue);
        var textView = CreateTextView(inputText, selectionStartLine, selectionStartColumn, selectionEndLine,
            selectionEndColumn);

        command.PreExec(textView, command.Targets.First());

        var expectedText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "");
        Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());

        AssertLinesSelected(textView, expectedText, selectionStartLine, selectionEndLine);
    }

    protected void AssertLinesSelected(IWpfTextView textView, TestText expectedText, int selectionStartLine,
        int? selectionEndLine)
    {
// commented line is selected
        Assert.Equal(expectedText.GetLineStartPosition(selectionStartLine), textView.Selection.Start.Position);
        Assert.Equal(expectedText.GetLineEndPosition(selectionEndLine ?? selectionStartLine),
            textView.Selection.End.Position);

        // cursor moved to end of selection
        Assert.Equal(textView.Selection.End.Position, textView.Caret.Position.BufferPosition.Position);
    }

    protected IWpfTextView CreateTextView(TestText inputText, int selectionStartLine, int selectionStartColumn,
        int? selectionEndLine, int? selectionEndColumn)
    {
        var textBuffer = VsxStubObjects.CreateTextBuffer(inputText.ToString(), VsContentTypes.FeatureFile);
        IWpfTextView textView = new StubWpfTextView(textBuffer);

        if (selectionEndColumn != null && selectionEndLine != null)
            textView.Selection.Select(new SnapshotSpan(
                    inputText.GetSnapshotPoint(textView.TextSnapshot, selectionStartLine, selectionStartColumn),
                    inputText.GetSnapshotPoint(textView.TextSnapshot, selectionEndLine.Value,
                        selectionEndColumn.Value)),
                false);

        int caretLine = selectionEndLine ?? selectionStartLine;
        int caretColumn = selectionEndColumn ?? selectionStartColumn;
        textView.Caret.MoveTo(inputText.GetSnapshotPoint(textView.TextSnapshot, caretLine, caretColumn));
        return textView;
    }
}
