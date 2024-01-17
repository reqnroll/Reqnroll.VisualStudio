#nullable disable
namespace Reqnroll.VisualStudio.Tests.Discovery;
/*

Undefined
=========
* No candidating step definitions => undef
* OUT: Some match, but with different type => error (now: undef)
* OUT: Some match, but with different scope => error (now: undef)
* SO, but all (incl. empty) undefined => undef

*/

public class ProjectBindingRegistryUndefinedTests : ProjectBindingRegistryTestsBase
{
    // No candidating step definitions => undef

    [Fact]
    public void Matches_undefined()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("not used step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my undefined step"), StubGherkinDocument.Instance);
        result.HasUndefined.Should().BeTrue();
    }

    //OUT: Some match, but with different type => error (now: undef)

    [Fact]
    public void Does_not_match_step_definition_of_a_different_type()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step", stepKeyword: StepKeyword.When),
            StubGherkinDocument.Instance);
        result.HasUndefined.Should().BeTrue();
    }

    //OUT: Some match, but with different scope => error (now: undef)

    [Fact]
    public void Does_not_match_tag_scoped_step_definition_if_not_tagged()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("my step", scope: CreateTagScope("mytag")));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my step"), StubGherkinDocument.Instance);
        result.HasUndefined.Should().BeTrue();
    }

    //SO, but all (incl. empty) undefined => undef

    [Fact]
    public void All_SO_examples_are_undefined()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("not used step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new[] {"cool", "other"}));
        result.HasUndefined.Should().BeTrue();
    }

    [Fact]
    public void Empty_SO_Examples()
    {
        _stepDefinitionBindings.Add(CreateStepDefinitionBinding("not used step"));
        var sut = CreateSut();

        var result = sut.MatchStep(CreateStep(text: "my <what> step"),
            CreateScenarioOutlineContext(null, null, "what", new string[0]));
        result.HasUndefined.Should().BeTrue();
    }
}
