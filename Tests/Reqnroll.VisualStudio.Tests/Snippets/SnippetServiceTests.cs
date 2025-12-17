using FluentAssertions;
using NSubstitute;
using Reqnroll.VisualStudio.Snippets;
using Reqnroll.VisualStudio.ProjectSystem;
using Reqnroll.VisualStudio.ProjectSystem.Configuration;
using Xunit;

namespace Reqnroll.VisualStudio.Tests.Snippets
{
    public class SnippetServiceTests
    {
        private readonly IProjectScope _projectScope;
        private readonly IIdeScope _ideScope;
        private readonly IDeveroomLogger _logger;
        private readonly SnippetService _service;
        private readonly DeveroomConfiguration _defaultDeveroomConfig;
        private readonly ITestOutputHelper testOutputHelper;

        public SnippetServiceTests(ITestOutputHelper testOutputHelper)
        {
            _ideScope = new StubIdeScope(testOutputHelper);
            _logger = new StubLogger();
            _projectScope = new StubProjectScope(@"C:\", "bin", _ideScope, new List<NuGetPackageReference>(), "net8");

            _defaultDeveroomConfig = new DeveroomConfiguration();

            // Construct the service with the substitute
            _service = new SnippetService(_projectScope);
            this.testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData(SnippetExpressionStyle.RegularExpression, "[Given(@\"pattern\")]\npublic void GivenPattern()\n{\nthrow new PendingStepException();\n}\n")]
        [InlineData(SnippetExpressionStyle.AsyncRegularExpression, "[Given(@\"pattern\")]\npublic async Task GivenPatternAsync()\n{\nthrow new PendingStepException();\n}\n")]
        [InlineData(SnippetExpressionStyle.CucumberExpression, "[Given(\"pattern\")]\npublic void GivenPattern()\n{\nthrow new PendingStepException();\n}\n")]
        [InlineData(SnippetExpressionStyle.AsyncCucumberExpression, "[Given(\"pattern\")]\npublic async Task GivenPatternAsync()\n{\nthrow new PendingStepException();\n}\n")]
        public void Generates_correct_step_definition_snippet(SnippetExpressionStyle style, string expectedSnippet)
        {
            // Arrange
            var undefinedStep = new DeveroomGherkinStep(new Gherkin.Ast.Location(0, 0), "Given ", Gherkin.StepKeywordType.Context, "pattern", null, StepKeyword.Given, ScenarioBlock.Given);
            var undefinedStepDescriptor = new UndefinedStepDescriptor(undefinedStep, "pattern");
            var indent = "";
            var newLine = "\n";

            // Act
            var snippet = _service.GetStepDefinitionSkeletonSnippet(
                undefinedStepDescriptor, style, indent, newLine);

            // Assert
            snippet.Should().Be(expectedSnippet);
        }
    }
}