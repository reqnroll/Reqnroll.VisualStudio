#pragma warning disable xUnit1026 //Theory method 'xxx' does not use parameter '_'

namespace Reqnroll.VisualStudio.Tests.Editor.Commands;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("../ApprovalTestData")]
public class DefineStepsCommandTests : CommandTestBase<DefineStepsCommand>
{
    public DefineStepsCommandTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper, (ps, tp) =>
                new DefineStepsCommand(ps.IdeScope, new StubBufferTagAggregatorFactoryService(tp), tp,
                    new StubEditorConfigOptionsProvider()),
            "ShowProblem: User Notification: ")
    {
    }

    private void ArrangePopup()
    {
        (ProjectScope.IdeScope.WindowManager as StubWindowManager)!
            .RegisterWindowAction<CreateStepDefinitionsDialogViewModel>(model =>
                model.Result = CreateStepDefinitionsDialogResult.Create);
    }

    [Fact]
    public async Task Warn_if_steps_have_been_defined_already()
    {
        var stepDefinition = ArrangeStepDefinition();
        var featureFile = ArrangeOneFeatureFile();

        var (_, command) = await ArrangeSut(stepDefinition, featureFile);
        var textView = CreateTextView(featureFile);

        Invoke(command, textView);

        WarningMessages().Should()
            .Contain(s => s.Contains("All steps have been defined in this file already."));
    }

    [Fact]
    public async Task CreateStepDefinitionsDialog_cancelled()
    {
        var stepDefinition = ArrangeStepDefinition(@"""I choose add""");
        var featureFile = ArrangeOneFeatureFile();

        var (_, command) = await ArrangeSut(stepDefinition, featureFile);
        var textView = CreateTextView(featureFile);

        Invoke(command, textView);

        ThereWereNoWarnings();
    }

    [Theory]
    [InlineData("01", @"I press add")]
    public async Task Step_definition_class_saved(string _, string expression)
    {
        var featureFile = ArrangeOneFeatureFile();

        ArrangePopup();
        var (_, command) = await ArrangeSut(TestStepDefinition.Void, featureFile);
        var textView = CreateTextView(featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        ThereWereNoWarnings();
        var createdStepDefinitionContent =
            ProjectScope.StubIdeScope.CurrentTextView.TextBuffer.CurrentSnapshot.GetText();
        Dump(ProjectScope.StubIdeScope.CurrentTextView, "Created stepDefinition file");
        createdStepDefinitionContent.Should().Contain(expression);

        await BindingRegistryIsModified(expression);
    }

    [Theory]
    [InlineData(ProjectType.Reqnroll, NamespaceStyle.BlockScoped)]
    [InlineData(ProjectType.Reqnroll, NamespaceStyle.FileScoped)]
    [InlineData(ProjectType.SpecFlow, NamespaceStyle.BlockScoped)]
    [InlineData(ProjectType.SpecFlow, NamespaceStyle.FileScoped)]
    public void GenerateStepDefinitionClass(ProjectType projectType, NamespaceStyle namespaceStyle)
    {
        // Arrange
        // Snippet should have single indentation (4 spaces) which will be used for file-scoped namespaces
        // and will have extra indentation added for block-scoped namespaces
        var snippet = """
            [When(@"I press add")]
            public void WhenIPressAdd()
            {
                throw new PendingStepException();
            }
        """;
        const string className = "Feature1StepDefinitions";
        const string @namespace = "MyNamespace.MyProject";

        var projectTraits = GetProjectTraits(projectType);
        var csharpConfig = new CSharpCodeGenerationConfiguration
        {
            NamespaceDeclarationStyle = GetNamespaceStyleValue(namespaceStyle)
        };

        // Act
        var result = DefineStepsCommand.GenerateStepDefinitionClass(
            snippet, className, @namespace, projectTraits, csharpConfig, "    ", Environment.NewLine);

        // Assert
        VerifyWithScenario(result, projectType, namespaceStyle);
    }

    private static void VerifyWithScenario(string result, ProjectType projectType, NamespaceStyle namespaceStyle)
    {
        var scenarioName = $"{projectType}_{namespaceStyle}";
        using (ApprovalResults.ForScenario(scenarioName))
        {
            Approvals.Verify(result);
        }
    }

    private static ReqnrollProjectTraits GetProjectTraits(ProjectType projectType)
    {
        return projectType switch
        {
            ProjectType.Reqnroll => ReqnrollProjectTraits.CucumberExpression | ReqnrollProjectTraits.MsBuildGeneration,
            ProjectType.SpecFlow => ReqnrollProjectTraits.LegacySpecFlow | ReqnrollProjectTraits.CucumberExpression | ReqnrollProjectTraits.MsBuildGeneration,
            _ => throw new ArgumentOutOfRangeException(nameof(projectType))
        };
    }

    private static string GetNamespaceStyleValue(NamespaceStyle namespaceStyle)
    {
        return namespaceStyle switch
        {
            NamespaceStyle.BlockScoped => "block_scoped",
            NamespaceStyle.FileScoped => "file_scoped",
            _ => throw new ArgumentOutOfRangeException(nameof(namespaceStyle))
        };
    }

    public enum ProjectType
    {
        Reqnroll,
        SpecFlow
    }

    public enum NamespaceStyle
    {
        BlockScoped,
        FileScoped
    }
}
