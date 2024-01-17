namespace Reqnroll.VisualStudio.Tests.Editor.Commands;

public class AutoFormatTableCommandTests
{
    private readonly TestText _expectedText = new(
        @"Feature: foo",
        @"Scenario: bar",
        @"Given table",
        @"    | foo    | bar   |",
        @"    | bazbaz | qux   |",
        @"    | qu\\   | c\n\| |",
        @"");

    private readonly StubIdeScope _ideScope;
    private readonly IDeveroomTaggerProvider _taggerProvider;

    private readonly TestText _unformattedText = new(
        @"Feature: foo", //12+2
        @"Scenario: bar", //13+2 (29)
        @"Given table", //11+2 (42)
        @"    | foo   |    bar  |", //21+2 (65)
        @"     | bazbaz | qux      |    ", //31+2 (98)
        @"|  qu\\| c\n\|     |", //20+2 (120)
        @"");

    public AutoFormatTableCommandTests(ITestOutputHelper testOutputHelper)
    {
        _ideScope = new StubIdeScope(testOutputHelper);
        _taggerProvider = new DeveroomTaggerProvider(_ideScope);
    }

    private StubWpfTextView CreateTextView(TestText inputText, string newLine) =>
        StubWpfTextView.CreateTextView(inputText,
            text => VsxStubObjects.CreateTextBuffer(text.ToString(newLine), VsContentTypes.FeatureFile));

    private StubWpfTextView CreateTextView(TestText inputText) =>
        CreateTextView(inputText, Environment.NewLine);

    [Fact]
    public void Formats_data_table_when_last_pipe_typed()
    {
        var command = CreateSUT();
        var inputText = _unformattedText.Replace(-2, "     |");

        var textView = CreateTextView(inputText);
        inputText.MoveCaretTo(textView, -2, -1);

        textView.SimulateType(command, '|', _taggerProvider);

        Assert.Equal(_expectedText.ToString(), textView.TextSnapshot.GetText());
    }

    [Fact]
    public void Formats_data_table_when_mid_pipe_typed()
    {
        var command = CreateSUT();
        var inputText = _unformattedText.Replace(-3, "bazbaz | qux", "bazbaz   qux");

        var textView = CreateTextView(inputText);
        inputText.MoveCaretTo(textView, -3, "     | bozboz".Length);

        textView.SimulateType(command, '|', _taggerProvider);

        Assert.Equal(_expectedText.ToString(), textView.TextSnapshot.GetText());
    }

    [Fact]
    public void Formats_data_table_when_pipe_typed_of_an_incomplete_table()
    {
        var command = CreateSUT();
        var inputText = _unformattedText.Replace(-2, @"| c\n\|     |"); // remove last cell

        var textView = CreateTextView(inputText);
        inputText.MoveCaretTo(textView, -2, -1);

        textView.SimulateType(command, '|', _taggerProvider);

        var expectedText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"Given table",
            @"    | foo    | bar |",
            @"    | bazbaz | qux |",
            @"    | qu\\   |",
            @"");
        Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());
    }

    [Fact]
    public void Should_not_remove_unfinished_cells_when_formatting_table()
    {
        var command = CreateSUT();
        var inputText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"Given table",
            @"    | foo |    bar  ",
            @"    | foo  |  bar baz  ",
            @"    | foo   ",
            @"    |   ",
            @"    | foo   |    ",
            @"    | foo   | \|   ",
            @"    | foo   | \|",
            @"    | foo   | bar \\|   ",
            @"    | foo   | bar ",
            @"");

        var textView = CreateTextView(inputText);
        inputText.MoveCaretTo(textView, -2, -1);

        textView.SimulateType(command, '|', _taggerProvider);

        var expectedText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"Given table",
            @"    | foo | bar",
            @"    | foo | bar baz",
            @"    | foo",
            @"    |",
            @"    | foo |",
            @"    | foo | \|",
            @"    | foo | \|",
            @"    | foo | bar \\ |",
            @"    | foo | bar    |",
            @"");
        Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());
    }

    [Fact]
    public void Should_not_format_data_table_when_masked_pipe_typed_of_an_incomplete_table()
    {
        var command = CreateSUT();
        var inputText = _unformattedText.Replace(-2, @"c\n\|     |", @" x\"); // remove last cell

        var textView = CreateTextView(inputText);
        inputText.MoveCaretTo(textView, -2, -1);

        textView.SimulateType(command, '|', _taggerProvider);

        Assert.Equal(inputText.ToString().Replace(@"x\", @"x\|"), textView.TextSnapshot.GetText());
    }

    [Theory]
    [InlineData("beginning of line", "", "")]
    [InlineData("before first cell", "  ", "  ")]
    [InlineData("right after pipe", "     |", "    | ")]
    [InlineData("at padding after pipe", "     | ", "    | ")]
    [InlineData("inside a cell", "     | baz", "    | baz")]
    [InlineData("inside a non-first cell", "     | bazbaz | q", "    | bazbaz | q")]
    [InlineData("non-existing cell position", "     | bazbaz | qux      ", "    | bazbaz | qux   ")]
    [InlineData("after last pipe", "     | bazbaz | qux       |", null)]
    [InlineData("end of line", null, null)]
    public void Position_cursor_after_formatting(string test, string caretColonStr, string expectedCaretColonStr)
    {
        _ideScope.Logger.LogInfo($"running: {test}");
        var caretColon = caretColonStr?.Length ?? -1;
        var expectedCaretColon = expectedCaretColonStr?.Length ?? -1;

        var command = CreateSUT();
        var inputText = _unformattedText;

        var textView = CreateTextView(inputText);
        inputText.MoveCaretTo(textView, -3, caretColon);

        command.PostExec(textView, '|'); // this does not enter the char itself

        Assert.Equal(_expectedText.ToString(), textView.TextSnapshot.GetText());
        // cursor position preserved
        _expectedText.AssertCaretAt(textView, -3, expectedCaretColon);
    }

    [Fact]
    public void Formats_table_with_empty_and_comment_lines()
    {
        var command = CreateSUT();
        var inputText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "Given table",
            "  | foo   |    bar  |",
            "  #comment",
            "     | bazbaz | qux      |    ",
            "   ", // empty line
            "|  quux| corge|     ",
            "");

        var textView = CreateTextView(inputText);
        inputText.MoveCaretTo(textView, -2, 0);

        command.PostExec(textView, '|'); // this does not enter the char itself

        var expectedText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "Given table",
            "  | foo    | bar   |",
            "  #comment",
            "  | bazbaz | qux   |",
            "   ",
            "  | quux   | corge |",
            "");

        Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());
    }

    [Fact]
    public void Uses_indent_of_first_line()
    {
        var command = CreateSUT();
        var inputText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "Given table",
            " | foo   |    bar  |",
            "     | bazbaz | qux      |    ",
            "|  quux| corge|     ",
            "");

        var textView = CreateTextView(inputText);
        inputText.MoveCaretTo(textView, -2, 0);

        command.PostExec(textView, '|'); // this does not enter the char itself

        var expectedText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "Given table",
            " | foo    | bar   |",
            " | bazbaz | qux   |",
            " | quux   | corge |",
            "");

        Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());
    }

    [Fact]
    public void Detects_line_ending()
    {
        var command = CreateSUT();
        var inputText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "Given table",
            " | foo   |    bar  |",
            "     | bazbaz | qux      |    ",
            "|  quux| corge|     ",
            "");

        var textView = CreateTextView(inputText);
        textView.StubEditorOptions.ReplicateNewLineCharacterOption = true;
        textView.StubEditorOptions.NewLineCharacterOption = "\n"; // not used, because it detects

        inputText.MoveCaretTo(textView, -2, 0);

        command.PostExec(textView, '|'); // this does not enter the char itself

        var expectedText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "Given table",
            " | foo    | bar   |",
            " | bazbaz | qux   |",
            " | quux   | corge |",
            "");

        Assert.Equal(expectedText.ToString(), textView.TextSnapshot.GetText());
    }

    [Fact]
    public void Uses_configured_line_ending()
    {
        var command = CreateSUT();
        var inputText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "Given table",
            " | foo   |    bar  |",
            "     | bazbaz | qux      |    ",
            "|  quux| corge|     ",
            "");

        var configuredNewLine = "\n";
        var textView = CreateTextView(inputText, configuredNewLine);
        textView.StubEditorOptions.ReplicateNewLineCharacterOption = false;
        textView.StubEditorOptions.NewLineCharacterOption = configuredNewLine;

        inputText.MoveCaretTo(textView, -2, 0);

        command.PostExec(textView, '|'); // this does not enter the char itself

        var expectedText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "Given table",
            " | foo    | bar   |",
            " | bazbaz | qux   |",
            " | quux   | corge |",
            "");

        Assert.Equal(expectedText.ToString(configuredNewLine), textView.TextSnapshot.GetText());
    }

    private AutoFormatTableCommand CreateSUT() =>
        new(
            _ideScope,
            new StubBufferTagAggregatorFactoryService(_taggerProvider),
            _taggerProvider,
            new GherkinDocumentFormatter(),
            new StubEditorConfigOptionsProvider()
        );
}
