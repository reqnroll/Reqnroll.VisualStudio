namespace Reqnroll.VisualStudio.Tests.Editor.Services;

public class GherkinDocumentFormatterTests
{
    private readonly GherkinFormatSettings _formatSettings = new();

    private GherkinDocumentFormatter CreateSUT() => new();

    private DeveroomGherkinDocument ParseGherkinDocument(TestText inputText)
    {
        var parser = new DeveroomGherkinParser(new ReqnrollGherkinDialectProvider("en-US"),
            Substitute.For<IMonitoringService>());
        parser.ParseAndCollectErrors(inputText.ToString(), new DeveroomNullLogger(), out var gherkinDocument, out _);
        return gherkinDocument;
    }

    private DocumentLinesEditBuffer GetLinesBuffer(TestText inputText) =>
        new(Substitute.For<ITextSnapshot>(), 0, inputText.Lines.Length - 1,
            inputText.Lines);

    [Fact]
    public void Should_not_remove_closing_delimiter_of_an_empty_docstring()
    {
        var sut = CreateSUT();
        var inputText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"    Given table",
            @"    ```",
            @"    ```",
            @"");
        var linesBuffer = GetLinesBuffer(inputText);

        sut.FormatGherkinDocument(ParseGherkinDocument(inputText), linesBuffer, _formatSettings);

        var expectedText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"    Given table",
            @"        ```",
            @"        ```",
            @"");
        Assert.Equal(expectedText.ToString(), linesBuffer.GetModifiedText(Environment.NewLine));
    }

    [Fact]
    public void Should_not_remove_empty_line_from_a_single_empty_line_docstring()
    {
        var sut = CreateSUT();
        var inputText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"    Given table",
            @"    ```",
            @"    ",
            @"    ```",
            @"");
        var linesBuffer = GetLinesBuffer(inputText);

        sut.FormatGherkinDocument(ParseGherkinDocument(inputText), linesBuffer, _formatSettings);

        var expectedText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"    Given table",
            @"        ```",
            @"        ",
            @"        ```",
            @"");
        Assert.Equal(expectedText.ToString(), linesBuffer.GetModifiedText(Environment.NewLine));
    }

    [Fact]
    public void Should_not_remove_whitespace_from_a_single_whitespace_line_docstring()
    {
        var sut = CreateSUT();
        var inputText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"    Given table",
            @"    ```",
            @"     ",
            @"    ```",
            @"");

        var linesBuffer = GetLinesBuffer(inputText);

        sut.FormatGherkinDocument(ParseGherkinDocument(inputText), linesBuffer, _formatSettings);

        var expectedText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"    Given table",
            @"        ```",
            @"         ",
            @"        ```",
            @"");
        Assert.Equal(expectedText.ToString(), linesBuffer.GetModifiedText(Environment.NewLine));
    }


    [Fact]
    public void Should_not_remove_unfinished_cells_when_formatting_table()
    {
        var sut = CreateSUT();
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
            @"    | foo   | bar |",
            @"");

        var linesBuffer = GetLinesBuffer(inputText);

        sut.FormatGherkinDocument(ParseGherkinDocument(inputText), linesBuffer, _formatSettings);

        var expectedText = new TestText(
            @"Feature: foo",
            @"Scenario: bar",
            @"    Given table",
            @"        | foo | bar",
            @"        | foo | bar baz",
            @"        | foo",
            @"        |",
            @"        | foo |",
            @"        | foo | \|",
            @"        | foo | \|",
            @"        | foo | bar \\ |",
            @"        | foo | bar    |",
            @"");
        Assert.Equal(expectedText.ToString(), linesBuffer.GetModifiedText(Environment.NewLine));
    }

    [Theory]
    // Header is 13 chars, so all cells will be padded to width 13
    [InlineData("| 123 |", true, "|         123 |")] // Right-align: only digits
    [InlineData("| abc123 |", true, "|      abc123 |")] // Right-align: mixed letters and digits
    [InlineData("| 12abc |", true, "|       12abc |")] // Right-align: digits at start
    [InlineData("| abc |", true, "| abc         |")] // Left-align: only letters
    [InlineData("| !@#4$% |", true, "|      !@#4$% |")] // Right-align: special chars and digit
    [InlineData("| !@#$% |", true, "| !@#$%       |")] // Left-align: only special chars
    [InlineData("| 123 |", false, "| 123         |")] // Left-align: only digits, but right-align disabled
    [InlineData("| abc123 |", false, "| abc123      |")] // Left-align: mixed, but right-align disabled
    public void Should_align_table_cells_based_on_content_and_setting(string tableRow, bool rightAlign, string expectedCell)
    {
        var sut = CreateSUT();
        var formatSettings = new GherkinFormatSettings();
        formatSettings.Configuration.TableCellRightAlignNumericContent = rightAlign;
        formatSettings.Configuration.TableCellPaddingSize = 1;
        formatSettings.Indent = "    ";
        var inputText = new TestText(
            "Feature: foo",
            "Scenario: bar",
            "    Given table",
            "    | HeaderValue |", // header row sets width to 13
            $"    {tableRow}",
            "");
        var linesBuffer = GetLinesBuffer(inputText);
        sut.FormatGherkinDocument(ParseGherkinDocument(inputText), linesBuffer, formatSettings);
        var formattedLine = linesBuffer.GetLineOneBased(5).TrimEnd().TrimStart(); // data row is now line 5
        Assert.Equal(expectedCell, formattedLine);
    }
}
