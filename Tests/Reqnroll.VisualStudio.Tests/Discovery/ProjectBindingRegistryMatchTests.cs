#nullable disable
namespace Reqnroll.VisualStudio.Tests.Discovery;

/*

Match
=====
* Matching step => jump
    * Unscoped
    * DataTable&DocString => select overload based on param type
    * Scoped
    * Unscoped&Scoped => use scoped or unscoped depending on contex (no ambiguity)
    * SO, but mathces to the same stepdef
* Binding errors (e.g. parameter count) => err,jump
* OUT: Parameter error (e.g. invalid conversion)
* OUT: Same method matches multiple ways

*/
public class ProjectBindingRegistryMatchTests : ProjectBindingRegistryTestsBase
{
    private MatchResultItem AssertSingleDefined(MatchResult match)
    {
        match.HasDefined.Should().BeTrue();
        match.HasSingleMatch.Should().BeTrue();
        return match.Items[0];
    }

    // Matching step (Unscoped) => jump

    [Fact]
    public void Matches_to_single_stepdef()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my other step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my cool step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my cool step", ScenarioBlock.When));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my cool step"), StubGherkinDocument.Instance);
        AssertSingleDefined(result);
    }

    [Fact]
    public void Matches_parameters()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my (.*) step",
            parameterTypes: GetParameterTypes("string")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my cool step"), StubGherkinDocument.Instance);
        var matchItem = AssertSingleDefined(result);
        matchItem.ParameterMatch.StepTextParameters.Should().HaveCount(1);
        matchItem.ParameterMatch.StepTextParameters.First().Index.Should().Be(3);
        matchItem.ParameterMatch.StepTextParameters.First().Length.Should().Be(4);
    }

    [Fact]
    public void Matches_parameters_with_multiple_capture_groups_used_by_cukeex_string_param()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my (?:(cool)|(bad)) step",
            parameterTypes: GetParameterTypes("string")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my cool step"), StubGherkinDocument.Instance);
        var matchItem = AssertSingleDefined(result);
        matchItem.ParameterMatch.StepTextParameters.Should().HaveCount(1);
        matchItem.ParameterMatch.StepTextParameters.First().Index.Should().Be(3);
        matchItem.ParameterMatch.StepTextParameters.First().Length.Should().Be(4);
    }

    [Fact]
    public void Matches_DataTable()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step",
            parameterTypes: GetParameterTypes("DataTable")));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step", stepArgument: CreateDataTable()),
            StubGherkinDocument.Instance);
        var matchItem = AssertSingleDefined(result);
        matchItem.ParameterMatch.MatchedDataTable.Should().BeTrue();
    }

    [Fact]
    public void Matches_DocString()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step",
            parameterTypes: GetParameterTypes("string")));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step", stepArgument: CreateDocString()),
            StubGherkinDocument.Instance);
        var matchItem = AssertSingleDefined(result);
        matchItem.ParameterMatch.MatchedDocString.Should().BeTrue();
    }

    [Fact]
    public void Matches_background_steps_with_feature_scope_when_there_are_no_scenarios()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step"), CreateEmptyFileBackgroundContext(null));
        AssertSingleDefined(result);
    }


    // Matching step (DataTable&DocString) => select overload based on param type

    [Fact]
    public void Selects_DataTable_from_DataTable_DocString_overloads()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step",
            parameterTypes: GetParameterTypes("DataTable")));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step",
            parameterTypes: GetParameterTypes("string")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step", stepArgument: CreateDataTable()),
            StubGherkinDocument.Instance);
        var matchItem = AssertSingleDefined(result);
        matchItem.ParameterMatch.MatchedDataTable.Should().BeTrue();
    }

    [Fact]
    public void Selects_DocString_from_DataTable_DocString_overloads()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step",
            parameterTypes: GetParameterTypes("DataTable")));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step",
            parameterTypes: GetParameterTypes("string")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step", stepArgument: CreateDocString()),
            StubGherkinDocument.Instance);
        var matchItem = AssertSingleDefined(result);
        matchItem.ParameterMatch.MatchedDocString.Should().BeTrue();
    }

    [Fact]
    public void Selects_prameterless_from_DataTable_DocString_prarameterless_overloads()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step",
            parameterTypes: GetParameterTypes("DataTable")));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step",
            parameterTypes: GetParameterTypes("string")));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step", stepArgument: null), StubGherkinDocument.Instance);
        AssertSingleDefined(result);
        result.HasErrors.Should().BeFalse();
    }

    // Matching step (Scoped) => jump

    [Fact]
    public void Matches_tag_scoped_step_definition_if_tagged_directly()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step", scope: CreateTagScope("@mytag")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step"), CreateScenarioContext(null, "@mytag"));
        AssertSingleDefined(result);
    }

    [Fact]
    public void Matches_tag_scoped_step_definition_if_tagged_in_an_upper_level()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step", scope: CreateTagScope("@featuretag")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step"), CreateScenarioContext(new[] {"@featuretag"}, "@mytag"));
        AssertSingleDefined(result);
    }

    // Matching step (Unscoped&Scoped) => use scoped or unscoped depending on contex (no ambiguity)

    [Fact]
    public void Selects_scoped_from_scoped_and_not_scoped_overloads()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step", scope: CreateTagScope("@mytag")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step"), CreateScenarioContext(null, "@mytag"));
        var matchItem = AssertSingleDefined(result);
        matchItem.MatchedStepDefinition.Scope.Tag.Should().NotBeNull();
    }

    [Fact]
    public void Selects_unscoped_from_scoped_and_not_scoped_overloads_if_not_tagged()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step", scope: CreateTagScope("@mytag")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step"), StubGherkinDocument.Instance);
        var matchItem = AssertSingleDefined(result);
        matchItem.MatchedStepDefinition.Scope.Should().BeNull();
    }

    // Matching step (SO, but mathces to the same stepdef) => jump

    [Fact]
    public void Matches_to_same_stepdef_in_SO()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my .* step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"cool", "other"}));
        AssertSingleDefined(result);
    }

    // Binding errors(e.g.parameter count) => err,jump

    [Fact]
    public void Indicates_invalid_parameter_count()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding(@"my invalid (\d+) with (.*) step",
            parameterTypes: GetParameterTypes("string")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my invalid 50 with extras step"), StubGherkinDocument.Instance);
        AssertSingleDefined(result);
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(m => m.Contains("parameter"));
    }

    [Theory]
    [InlineData("I press add", "I press add")]
    [InlineData("I press (.*)", "I press add")]
    public void Match_after_ReplaceStepDefinition(string expression, string stepText)
    {
        var original = CreateStepDefinitionBinding("original");
        var replacement = CreateStepDefinitionBinding(expression);
        MatchAfterReplaceStepDefinition(expression, stepText, original, replacement);
    }

    [Theory]
    [InlineData("I press add", "I press add")]
    [InlineData("I press (.*)", "I press add")]
    public void Match_after_ReplaceStepDefinition_WithSpecifiedExpression(string expression, string stepText)
    {
        var original = CreateStepDefinitionBinding("original");
        var replacement = original.WithSpecifiedExpression(expression);
        MatchAfterReplaceStepDefinition(expression, stepText, original, replacement);
    }

    private void MatchAfterReplaceStepDefinition(string expression, string stepText,
        ProjectStepDefinitionBinding original,
        ProjectStepDefinitionBinding replacement)
    {
        _stepDefinitionBindings.Add(original);
        var sut = CreateSut();
        var modifiedSut = sut.ReplaceStepDefinition(original, replacement);

        var result = modifiedSut.MatchStep(CreateStep(text: stepText), StubGherkinDocument.Instance);
        var matchItem = AssertSingleDefined(result);
        matchItem.MatchedStepDefinition.Expression.Should().Be(expression);
        matchItem.MatchedStepDefinition.Regex.ToString().Should().Be($"^{expression}$");
    }
}
