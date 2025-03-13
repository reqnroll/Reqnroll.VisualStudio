using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Tests.Editor.Services;

public class DeveroomGherkinParserTests
{
    [Fact]
    public void Should_provide_parse_result_when_unexpected_end_of_file()
    {
        var sut = new DeveroomGherkinParser(new ReqnrollGherkinDialectProvider("en-US"),
            Substitute.For<IMonitoringService>());

        var result = sut.ParseAndCollectErrors(@"
Feature: Addition
@tag
",
            new DeveroomNullLogger(), out var gherkinDocument, out var errors);
        gherkinDocument.Should().NotBeNull();
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_tolerate_backslash_at_end_of_line_in_DataTable()
    {
        var sut = new DeveroomGherkinParser(new ReqnrollGherkinDialectProvider("en-US"),
            Substitute.For<IMonitoringService>());

        var result = sut.ParseAndCollectErrors(@"
Feature: Addition
Scenario: Add two numbers
	When I press
		| foo |
		| bar \
",
            new DeveroomNullLogger(), out var gherkinDocument, out var errors);
        gherkinDocument.Should().NotBeNull();
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_tolerate_unfinished_DataTable()
    {
        var sut = new DeveroomGherkinParser(new ReqnrollGherkinDialectProvider("en-US"),
            Substitute.For<IMonitoringService>());

        var result = sut.ParseAndCollectErrors(@"
Feature: Addition
Scenario: Add two numbers
	When I press
		| foo |
		| bar
",
            new DeveroomNullLogger(), out var gherkinDocument, out var errors);
        gherkinDocument.Should().NotBeNull();
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_provide_parse_result_when_file_ends_with_open_docstring()
    {
        var sut = new DeveroomGherkinParser(new ReqnrollGherkinDialectProvider("en-US"),
            Substitute.For<IMonitoringService>());

        var result = sut.ParseAndCollectErrors(@"
Feature: Addition
Scenario: Add two numbers
  Given I have added
    ```
",
            new DeveroomNullLogger(), out var gherkinDocument, out var errors);
        gherkinDocument.Should().NotBeNull();
        result.Should().BeFalse();
    }
}
