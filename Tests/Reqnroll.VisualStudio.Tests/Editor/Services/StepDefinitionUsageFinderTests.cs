#nullable disable

namespace Reqnroll.VisualStudio.Tests.Editor.Services;

public class StepDefinitionUsageFinderTests
{
    private readonly DeveroomConfiguration _configuration = new();
    private readonly string _featureFilePath = "SampleFeature.feature";
    private readonly ITestOutputHelper _testOutputHelper;

    public StepDefinitionUsageFinderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private ProjectStepDefinitionBinding CreateStepDefinitionBinding(string regex, params string[] parameterTypes)
    {
        parameterTypes = parameterTypes ?? new string[0];
        return new ProjectStepDefinitionBinding(ScenarioBlock.Given, new Regex("^" + regex + "$"), null,
            new ProjectStepDefinitionImplementation("M1", parameterTypes, null));
    }

    private ProjectStepDefinitionBinding[] CreateStepDefinitionBindings(string regex, params string[] parameterTypes)
    {
        return new[] {CreateStepDefinitionBinding(regex, parameterTypes)};
    }

    private StepDefinitionUsageFinder CreateSut()
    {
        var ideScope = new StubIdeScope(_testOutputHelper);
        return new StepDefinitionUsageFinder(ideScope);
    }

    [Fact]
    public void Finds_step_definition_in_a_normal_scenario()
    {
        var sut = CreateSut();

        var projectStepDefinitionBindings = CreateStepDefinitionBindings("I did something");
        var featureFile = @"Feature: Sample feature
Scenario: Sample scenario
    Given I did something
";
        var result = sut
            .FindUsagesFromContent(projectStepDefinitionBindings, featureFile, _featureFilePath, _configuration)
            .ToArray();

        result.Should().HaveCount(1);
        result[0].SourceLocation.SourceFile.Should().Be(_featureFilePath);
        result[0].SourceLocation.SourceFileLine.Should().Be(3);
        result[0].Step.Should().NotBeNull();
        result[0].Step.Keyword.Should().Be("Given ");
        result[0].Step.Text.Should().Be("I did something");
    }

    [Fact]
    public void Finds_step_definition_in_multiple_scenarios()
    {
        var sut = CreateSut();

        var projectStepDefinitionBindings = CreateStepDefinitionBindings("I did something");
        var featureFile = @"Feature: Sample feature
Scenario: Sample scenario
    Given I did something
    And I did something else
    And I did something
Scenario: Other scenario
    Given I did something
";
        var result = sut
            .FindUsagesFromContent(projectStepDefinitionBindings, featureFile, _featureFilePath, _configuration)
            .ToArray();

        result.Should().HaveCount(3);
        result[0].SourceLocation.SourceFileLine.Should().Be(3);
        result[1].SourceLocation.SourceFileLine.Should().Be(5);
        result[2].SourceLocation.SourceFileLine.Should().Be(7);
    }

    [Fact]
    public void Finds_step_definition_in_scenario_outline()
    {
        var sut = CreateSut();

        var projectStepDefinitionBindings = CreateStepDefinitionBindings("I did something");
        var featureFile = @"Feature: Sample feature
Scenario Outline: Sample scenario
    Given I did <what>
Examples:
    | what      |
    | something |
    | undefined |
";
        var result = sut
            .FindUsagesFromContent(projectStepDefinitionBindings, featureFile, _featureFilePath, _configuration)
            .ToArray();

        result.Should().HaveCount(1);
        result[0].SourceLocation.SourceFile.Should().Be(_featureFilePath);
        result[0].SourceLocation.SourceFileLine.Should().Be(3);
    }

    [Fact]
    public void Finds_step_definition_in_background()
    {
        var sut = CreateSut();

        var projectStepDefinitionBindings = CreateStepDefinitionBindings("I did something");
        var featureFile = @"Feature: Sample feature
Background: 
    Given I did something
Scenario: Sample scenario
    Given I did something else
";
        var result = sut
            .FindUsagesFromContent(projectStepDefinitionBindings, featureFile, _featureFilePath, _configuration)
            .ToArray();

        result.Should().HaveCount(1);
        result[0].SourceLocation.SourceFile.Should().Be(_featureFilePath);
        result[0].SourceLocation.SourceFileLine.Should().Be(3);
    }

    [Fact]
    public void Finds_step_definition_in_non_english_feature_file()
    {
        var sut = CreateSut();

        var projectStepDefinitionBindings = CreateStepDefinitionBindings("I did something");
        var featureFile = @"#language: hu-HU
Jellemző: Sample feature
Forgatókönyv: Sample scenario
    Amennyiben I did something
";
        var result = sut
            .FindUsagesFromContent(projectStepDefinitionBindings, featureFile, _featureFilePath, _configuration)
            .ToArray();

        result.Should().HaveCount(1);
    }

    [Fact]
    public void Finds_step_definition_in_non_english_feature_file_using_project_default_language()
    {
        var sut = CreateSut();

        var projectStepDefinitionBindings = CreateStepDefinitionBindings("I did something");
        var featureFile = @"Jellemző: Sample feature
Forgatókönyv: Sample scenario
    Amennyiben I did something
";
        _configuration.DefaultFeatureLanguage = "hu-HU";

        var result = sut
            .FindUsagesFromContent(projectStepDefinitionBindings, featureFile, _featureFilePath, _configuration)
            .ToArray();

        result.Should().HaveCount(1);
    }
}
