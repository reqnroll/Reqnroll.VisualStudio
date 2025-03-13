using NSubstitute;

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
}
