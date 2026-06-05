#nullable disable
using Reqnroll.VisualStudio.Editor.Services.StepDefinitions;

namespace Reqnroll.VisualStudio.Tests.Editor.Services.StepDefinitions;

public class RegexStepDefinitionExpressionAnalyzerTests
{
    [Fact]
    public void Parse_SimpleText_ReturnsSimpleTextPart()
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse("I press add");

        result.Parts.Should().HaveCount(1);
        result.Parts[0].Should().BeOfType<AnalyzedStepDefinitionExpressionSimpleTextPart>();
        var part = (AnalyzedStepDefinitionExpressionSimpleTextPart)result.Parts[0];
        part.Text.Should().Be("I press add");
        part.UnescapedText.Should().Be("I press add");
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmptySimpleTextPart()
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse("");

        result.Parts.Should().HaveCount(1);
        result.Parts[0].Should().BeOfType<AnalyzedStepDefinitionExpressionSimpleTextPart>();
        var part = (AnalyzedStepDefinitionExpressionSimpleTextPart)result.Parts[0];
        part.Text.Should().Be("");
        part.UnescapedText.Should().Be("");
    }

    [Theory]
    [InlineData("I have (.*) apples", "I have ", "(.*)", " apples")]
    [InlineData("(.*) is entered", "", "(.*)", " is entered")]
    [InlineData("I press (.*)", "I press ", "(.*)", "")]
    [InlineData("I have (.*) and (.*) apples", "I have ", "(.*)", " and ", "(.*)", " apples")]
    public void Parse_WithCapturingGroups_SplitsIntoPartsCorrectly(string expression, params string[] expectedParts)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(expectedParts.Length);
        for (int i = 0; i < expectedParts.Length; i++)
        {
            result.Parts[i].ExpressionText.Should().Be(expectedParts[i], $"part {i} should match");
        }
    }

    [Fact]
    public void Parse_WithCapturingGroup_ReturnsParameterPart()
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse("I have (.*) apples");

        result.Parts.Should().HaveCount(3);
        result.Parts[0].Should().BeOfType<AnalyzedStepDefinitionExpressionSimpleTextPart>();
        result.Parts[1].Should().BeOfType<AnalyzedStepDefinitionExpressionParameterPart>();
        result.Parts[2].Should().BeOfType<AnalyzedStepDefinitionExpressionSimpleTextPart>();

        var paramPart = (AnalyzedStepDefinitionExpressionParameterPart)result.Parts[1];
        paramPart.ParameterExpression.Should().Be("(.*)");
    }

    [Theory]
    [InlineData(@"some \(context\)", @"some \(context\)", "some (context)")]
    [InlineData(@"some \{context\}", @"some \{context\}", "some {context}")]
    [InlineData(@"some \[context\]", @"some \[context\]", "some [context]")]
    [InlineData(@"chars \\\*\+\?\|\{\}\[\]\(\)\^\$\#", @"chars \\\*\+\?\|\{\}\[\]\(\)\^\$\#", @"chars \*+?|{}[]()^$#")]
    public void Parse_WithEscapedChars_UnescapesCorrectly(string expression, string expectedText, string expectedUnescapedText)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(1);
        var part = (AnalyzedStepDefinitionExpressionSimpleTextPart)result.Parts[0];
        part.Text.Should().Be(expectedText);
        part.UnescapedText.Should().Be(expectedUnescapedText);
    }

    [Theory]
    [InlineData(@"some \[context] (.*)", @"some \[context] ", "some [context] ", "(.*)", "")]
    [InlineData(@"foo \\ (.+) bar", @"foo \\ ", @"foo \ ", "(.+)", " bar")]
    public void Parse_WithEscapedCharsAndParameters_ProcessesBothCorrectly(string expression, string expectedText1, string expectedUnescaped1, string expectedParam, string expectedText2)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(3);
        var part1 = (AnalyzedStepDefinitionExpressionSimpleTextPart)result.Parts[0];
        part1.Text.Should().Be(expectedText1);
        part1.UnescapedText.Should().Be(expectedUnescaped1);

        var paramPart = (AnalyzedStepDefinitionExpressionParameterPart)result.Parts[1];
        paramPart.ParameterExpression.Should().Be(expectedParam);

        var part2 = (AnalyzedStepDefinitionExpressionSimpleTextPart)result.Parts[2];
        part2.ExpressionText.Should().Be(expectedText2);
    }

    [Theory]
    [InlineData("(?:non-capturing) (.*)", "(?:non-capturing) ", "(.*)", "")]
    [InlineData("foo (?:bar) (.*)", "foo (?:bar) ", "(.*)", "")]
    [InlineData("(?:a|b) and (?:c|d)", "(?:a|b) and (?:c|d)")]
    public void Parse_WithNonCapturingGroups_TreatsAsText(string expression, params string[] expectedParts)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(expectedParts.Length);
        for (int i = 0; i < expectedParts.Length; i++)
        {
            result.Parts[i].ExpressionText.Should().Be(expectedParts[i], $"part {i} should match");
        }
    }

    [Theory]
    [InlineData(@"foo (\d+) bar", "foo ", @"(\d+)", " bar")]
    [InlineData(@"foo (?<hello>.(.)) bar", "foo ", @"(?<hello>.(.))", " bar")]
    [InlineData(@"foo (?<name>\d+) bar", "foo ", @"(?<name>\d+)", " bar")]
    public void Parse_WithNestedGroups_CapturesEntireGroup(string expression, params string[] expectedParts)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(expectedParts.Length);
        for (int i = 0; i < expectedParts.Length; i++)
        {
            result.Parts[i].ExpressionText.Should().Be(expectedParts[i], $"part {i} should match");
        }
    }

    [Theory]
    [InlineData(@"foo (?<hello>.\)(.)) bar", "foo ", @"(?<hello>.\)(.))", " bar")]
    [InlineData(@"foo (a\)b) bar", "foo ", @"(a\)b)", " bar")]
    public void Parse_WithEscapedParenthesesInGroup_HandlesCorrectly(string expression, params string[] expectedParts)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(expectedParts.Length);
        for (int i = 0; i < expectedParts.Length; i++)
        {
            result.Parts[i].ExpressionText.Should().Be(expectedParts[i], $"part {i} should match");
        }
    }

    [Theory]
    [InlineData("foo? (.*) bar", "foo? ", "(.*)", " bar")]
    [InlineData("foo+ (.*) bar", "foo+ ", "(.*)", " bar")]
    [InlineData("foo* (.*) bar", "foo* ", "(.*)", " bar")]
    [InlineData("foo. (.*) bar", "foo. ", "(.*)", " bar")]
    [InlineData("foo[a-z] (.*) bar", "foo[a-z] ", "(.*)", " bar")]
    [InlineData("foo|bar (.*) baz", "foo|bar ", "(.*)", " baz")]
    [InlineData("^foo (.*) bar$", "^foo ", "(.*)", " bar$")]
    [InlineData("foo#comment (.*)", "foo#comment ", "(.*)", "")]
    public void Parse_WithRegexOperators_CreatesWithOperatorsTextPart(string expression, params string[] expectedParts)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(expectedParts.Length);

        // First part should be WithOperators type
        result.Parts[0].Should().BeOfType<AnalyzedStepDefinitionExpressionWithOperatorsTextPart>();
        result.Parts[0].ExpressionText.Should().Be(expectedParts[0]);

        // Verify all parts match
        for (int i = 0; i < expectedParts.Length; i++)
        {
            result.Parts[i].ExpressionText.Should().Be(expectedParts[i], $"part {i} should match");
        }
    }

    [Theory]
    [InlineData("(very basic|standard|scientific)", "(very basic|standard|scientific)")]
    [InlineData("( 1st| 2nd | 3 rd |4th)", "( 1st| 2nd | 3 rd |4th)")]
    [InlineData("(a|b|c)", "(a|b|c)")]
    public void Parse_WithPipeInCapturingGroup_RecognizesAsParameter(string expression, string expectedParam)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(3); // empty text + parameter + empty text
        result.Parts[1].Should().BeOfType<AnalyzedStepDefinitionExpressionParameterPart>();
        var paramPart = (AnalyzedStepDefinitionExpressionParameterPart)result.Parts[1];
        paramPart.ParameterExpression.Should().Be(expectedParam);
    }

    [Theory]
    [InlineData("(.*) is entered into the (very basic|standard|scientific) calculator", "", "(.*)", " is entered into the ", "(very basic|standard|scientific)", " calculator")]
    [InlineData("(.*) saved with name ([^']*)", "", "(.*)", " saved with name ", "([^']*)", "")]
    public void Parse_MultipleParametersWithPipe_RecognizesBothAsParameters(string expression, params string[] expectedParts)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(expectedParts.Length);
        for (int i = 0; i < expectedParts.Length; i++)
        {
            result.Parts[i].ExpressionText.Should().Be(expectedParts[i], $"part {i} should match");
        }
    }

    [Theory]
    [InlineData("(?i)foo bar", "(?i)foo bar")]
    [InlineData("(?i)foo (.*) bar", "(?i)foo ", "(.*)", " bar")]
    [InlineData("(?m)^start", "(?m)^start")]
    [InlineData("(?s)foo.bar", "(?s)foo.bar")]
    public void Parse_WithInlineRegexOptions_IncludesOptionsInTextPart(string expression, params string[] expectedParts)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(expectedParts.Length);
        for (int i = 0; i < expectedParts.Length; i++)
        {
            result.Parts[i].ExpressionText.Should().Be(expectedParts[i], $"part {i} should match");
        }

        // The (?i) should be in a WithOperators part, not treated as a non-capturing group
        if (expression.StartsWith("(?i)") || expression.StartsWith("(?m)") || expression.StartsWith("(?s)"))
        {
            result.Parts[0].Should().BeOfType<AnalyzedStepDefinitionExpressionWithOperatorsTextPart>();
        }
    }

    [Theory]
    [InlineData("(?i:foo) bar", "(?i:foo) bar")]
    [InlineData("(?i:foo) (.*) bar", "(?i:foo) ", "(.*)", " bar")]
    [InlineData("(?i:a)nd (.*) into the calculator", "(?i:a)nd ", "(.*)", " into the calculator")]
    [InlineData("foo (?i:BAR) (.*)", "foo (?i:BAR) ", "(.*)", "")]
    public void Parse_WithInlineRegexOptionsGroup_IncludesInTextPart(string expression, params string[] expectedParts)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(expectedParts.Length);
        for (int i = 0; i < expectedParts.Length; i++)
        {
            result.Parts[i].ExpressionText.Should().Be(expectedParts[i], $"part {i} should match");
        }

        // Verify that inline options groups are not treated as parameters
        var paramParts = result.Parts.OfType<AnalyzedStepDefinitionExpressionParameterPart>().ToList();
        paramParts.Should().NotContain(p => p.ParameterExpression.StartsWith("(?i:"));
    }

    [Theory]
    [InlineData("(?i:a)nd (?i:b)ut (.*)", "(?i:a)nd (?i:b)ut ", "(.*)", "")]
    [InlineData("(?:foo)(?i:bar)(.*)", "(?:foo)(?i:bar)", "(.*)", "")]
    public void Parse_WithMultipleInlineOptionsGroups_HandlesCorrectly(string expression, params string[] expectedParts)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(expectedParts.Length);
        for (int i = 0; i < expectedParts.Length; i++)
        {
            result.Parts[i].ExpressionText.Should().Be(expectedParts[i], $"part {i} should match");
        }
    }

    [Theory]
    [InlineData("(?im:foo) (.*)", "(?im:foo) ", "(.*)", "")]
    [InlineData("(?i-m:foo) (.*)", "(?i-m:foo) ", "(.*)", "")]
    [InlineData("(?imnsx:test) bar", "(?imnsx:test) bar")]
    public void Parse_WithMultipleRegexOptionFlags_RecognizesAsNonCapturingGroup(string expression, params string[] expectedParts)
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse(expression);

        result.Parts.Should().HaveCount(expectedParts.Length);
        for (int i = 0; i < expectedParts.Length; i++)
        {
            result.Parts[i].ExpressionText.Should().Be(expectedParts[i], $"part {i} should match");
        }

        // Verify these are not treated as parameters
        var paramParts = result.Parts.OfType<AnalyzedStepDefinitionExpressionParameterPart>().ToList();
        paramParts.Should().NotContain(p => p.ParameterExpression.Contains("(?i"));
    }

    [Fact]
    public void Parse_WithUnmatchedOpeningParenthesis_HandlesGracefully()
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse("foo (.*");

        result.Parts.Should().HaveCount(3);
        result.Parts[0].Should().BeOfType<AnalyzedStepDefinitionExpressionSimpleTextPart>();
        result.Parts[1].Should().BeOfType<AnalyzedStepDefinitionExpressionParameterPart>();
        var paramPart = (AnalyzedStepDefinitionExpressionParameterPart)result.Parts[1];
        paramPart.ParameterExpression.Should().Be("(.*"); // Captures to end of string
    }

    [Fact]
    public void Parse_ContainsOnlySimpleText_ReturnsTrueForSimpleText()
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse("I press add");

        result.ContainsOnlySimpleText.Should().BeTrue();
    }

    [Fact]
    public void Parse_ContainsOnlySimpleText_ReturnsTrueForSimpleTextWithParameters()
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse("I have (.*) apples");

        result.ContainsOnlySimpleText.Should().BeTrue();
    }

    [Fact]
    public void Parse_ContainsOnlySimpleText_ReturnsFalseForRegexOperators()
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse("foo? (.*) bar");

        result.ContainsOnlySimpleText.Should().BeFalse();
    }

    [Fact]
    public void Parse_ParameterParts_ReturnsOnlyParameterParts()
    {
        var sut = new RegexStepDefinitionExpressionAnalyzer();

        var result = sut.Parse("I have (.*) and (\\d+) items");

        var paramParts = result.ParameterParts.ToList();
        paramParts.Should().HaveCount(2);
        paramParts[0].ParameterExpression.Should().Be("(.*)");
        paramParts[1].ParameterExpression.Should().Be("(\\d+)");
    }
}