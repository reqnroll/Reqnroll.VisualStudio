#nullable disable


namespace Reqnroll.VisualStudio.Tests.Discovery;

/*

Multi-match (SO, Background)
============================
* All match => list
* Some with binding/parameter errors => err,list
* Some undefined => undef,list incl. generate step def
* Some ambiguous => err,list

*/
public class ProjectBindingRegistryMultiMatchTests : ProjectBindingRegistryTestsBase
{
    // All match => list

    [Fact]
    public void Matches_multiple_stepdefs_in_SO()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my cool step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my other step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"cool", "other"}));
        result.HasMultipleMatches.Should().BeTrue();
        result.Items.All(r => r.Type == MatchResultType.Defined).Should().BeTrue();
    }

    [Fact]
    public void Removes_duplicated_stepdefs_in_SO()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my c.* step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my other step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"cool", "other", "colorful"}));
        result.HasMultipleMatches.Should().BeTrue();
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Includes_tagged_examples_in_scope_matching_in_SO()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my scoped step", scope: CreateTagScope("@mytag")));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my other step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"scoped", "other"}, new[] {"@mytag"}));
        result.HasMultipleMatches.Should().BeTrue();
        result.Items.All(r => r.Type == MatchResultType.Defined).Should().BeTrue();
    }

    [Fact]
    public void Matches_multiple_stepdefs_in_Background()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step", scope: CreateTagScope("@mytag")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step"), CreateBackgroundContext(null, new[] {"@mytag"}));
        result.HasMultipleMatches.Should().BeTrue();
        result.Items.All(r => r.Type == MatchResultType.Defined).Should().BeTrue();
    }

    [Fact]
    public void Includes_tagged_examples_in_scope_matching_for_Background()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step", scope: CreateTagScope("@mytag")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step"),
            CreateBackgroundContext(outlineExamplesTags: new[] {"@mytag"}));
        result.HasMultipleMatches.Should().BeTrue();
        result.Items.All(r => r.Type == MatchResultType.Defined).Should().BeTrue();
    }

    // Some with binding/parameter errors => err,list

    [Fact]
    public void Includes_invalid_match_in_result_in_SO()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my cool step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding(@"my invalid (\d+) with (.*) step",
            parameterTypes: GetParameterTypes("string")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"cool", "invalid 50 with extras"}));
        result.HasMultipleMatches.Should().BeTrue();
        result.Items.All(r => r.Type == MatchResultType.Defined).Should().BeTrue();
        result.Items.Should().Contain(m => m.HasErrors);
        result.HasErrors.Should().BeTrue();
    }

    // Some undefined => undef,list incl. generate step def

    [Fact]
    public void Includes_undefined_in_result_in_SO()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my cool step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"cool", "other"}));
        result.HasMultipleMatches.Should().BeTrue();
        result.Items.Should().Contain(m => m.Type == MatchResultType.Undefined);
    }

    [Fact]
    public void Includes_max_1_undefined_in_result_in_SO()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my cool step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"cool", "other", "third"}));
        result.HasMultipleMatches.Should().BeTrue();
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(m => m.Type == MatchResultType.Undefined);
    }

    // Some ambiguous => err,list

    [Fact]
    public void Includes_ambiguous_in_result_in_SO()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my cool step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my o.* step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my oth.* step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"cool", "other"}));
        result.HasMultipleMatches.Should().BeTrue();
        result.Items.Should().Contain(m => m.Type == MatchResultType.Ambiguous);
    }

    [Fact]
    public void Includes_max_1_ambiguous_with_all_candidates_in_result_in_SO()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my cool step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my o.* step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my oth.* step"));
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my or.* step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"cool", "other", "organic"}));
        result.HasMultipleMatches.Should().BeTrue();
        result.Items.Should().HaveCount(4);
        result.Items.Should().Contain(m => m.Type == MatchResultType.Ambiguous);
        result.Items.Where(m => m.Type == MatchResultType.Ambiguous).Should().HaveCount(3);
    }
}
