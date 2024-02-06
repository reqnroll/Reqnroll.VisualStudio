#nullable disable
using System;
using System.Linq;
using Reqnroll.VisualStudio.Editor.Completions;

namespace Reqnroll.VisualStudio.Tests.Editor.Completions;

public class StepDefinitionSamplerTests
{
    private ProjectStepDefinitionBinding CreateStepDefinitionBinding(string regex, params string[] parameterTypes)
    {
        parameterTypes = parameterTypes ?? new string[0];
        return new ProjectStepDefinitionBinding(ScenarioBlock.Given, new Regex("^" + regex + "$"), null,
            new ProjectBindingImplementation("M1", parameterTypes, null));
    }

    [Fact]
    public void Uses_regex_core_for_simple_stepdefs()
    {
        var sut = new StepDefinitionSampler();

        var result = sut.GetStepDefinitionSample(CreateStepDefinitionBinding("I press add"));

        result.Should().Be("I press add");
    }

    [Theory]
    [InlineData("I have entered (.*) into the calculator", "I have entered [int] into the calculator", "System.Int32")]
    [InlineData("(.*) is entered into the calculator", "[int] is entered into the calculator", "System.Int32")]
    [InlineData("what I have entered into the calculator is (.*)", "what I have entered into the calculator is [int]",
        "System.Int32")]
    [InlineData("I have entered (.*) into the calculator", "I have entered [string] into the calculator",
        "System.String")]
    [InlineData("I have entered (.*) into the calculator", "I have entered [Version] into the calculator",
        "System.Version")]
    [InlineData("I have entered (.*) and (.*) into the calculator",
        "I have entered [int] and [???] into the calculator", "System.Int32")]
    [InlineData("I have entered (.*) and ([^\"]*) into the calculator",
        "I have entered [int] and [string] into the calculator", "System.Int32", "System.String")]
    public void Emits_param_placeholders(string regex, string expectedResult, params string[] paramType)
    {
        var sut = new StepDefinitionSampler();

        var result = sut.GetStepDefinitionSample(CreateStepDefinitionBinding(regex, paramType));

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(@"some \(context\)", @"some (context)")]
    [InlineData(@"some \{context\}", @"some {context}")]
    [InlineData(@"some \[context\]", @"some [context]")]
    [InlineData(@"some \[context]", @"some [context]")]
    [InlineData(@"some \[context] (.*)", @"some [context] [???]")]
    [InlineData(@"chars \\\*\+\?\|\{\}\[\]\(\)\^\$\#", @"chars \*+?|{}[]()^$#")]
    public void Unescapes_masked_chars(string regex, string expectedResult)
    {
        var sut = new StepDefinitionSampler();

        var result = sut.GetStepDefinitionSample(CreateStepDefinitionBinding(regex));

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(@"foo (\d+) bar", @"foo [int] bar")]
    [InlineData(@"foo (?<hello>.(.)) bar", @"foo [int] bar")]
    [InlineData(@"foo (?<hello>.\)(.)) bar", @"foo [int] bar")]
    public void Allows_nested_groups(string regex, string expectedResult)
    {
        var sut = new StepDefinitionSampler();

        var result = sut.GetStepDefinitionSample(CreateStepDefinitionBinding(regex, "System.Int32"));

        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(@"foo? (\d+) bar")]
    [InlineData(@"foo (?:\d+) bar")]
    [InlineData(@"foo [a-z] bar")]
    [InlineData(@"foo. (\d+) bar")]
    [InlineData(@"foo* (\d+) bar")]
    [InlineData(@"foo+ (\d+) bar")]
    public void Falls_back_to_regex(string regex)
    {
        var sut = new StepDefinitionSampler();

        var result = sut.GetStepDefinitionSample(CreateStepDefinitionBinding(regex, "System.Int32"));

        result.Should().Be(regex);
    }
}
