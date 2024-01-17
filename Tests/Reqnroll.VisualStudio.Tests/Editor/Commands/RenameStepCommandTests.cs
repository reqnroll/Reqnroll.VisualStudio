#pragma warning disable xUnit1026 //Theory method 'xxx' does not use parameter '_'

namespace Reqnroll.VisualStudio.Tests.Editor.Commands;

public class RenameStepCommandTests : CommandTestBase<RenameStepCommand>
{
    public RenameStepCommandTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper,
        (ps, tp) => new RenameStepCommand(ps.IdeScope, new StubBufferTagAggregatorFactoryService(tp),
            tp),
        "User Notification: The following problems occurred:" + Environment.NewLine)
    {
        ProjectScope.StubIdeScope.TextViewFactory = (TestText inputText, string filePath)
            => StubWpfTextView.CreateTextView(inputText, text =>
            {
                var textBuffer = VsxStubObjects.CreateTextBuffer(text.ToString(), VsContentTypes.CSharp);
                textBuffer.Properties.AddProperty(typeof(IProjectScope), ProjectScope);
                textBuffer.Properties.AddProperty(typeof(IVsTextBuffer), new FilePathProvider(filePath));
                return textBuffer;
            });
    }

    private void ArrangePopup(string modelStepText)
    {
        (ProjectScope.IdeScope.WindowManager as StubWindowManager)!
            .RegisterWindowAction<RenameStepViewModel>(model => model.StepText = modelStepText);
    }

    [Fact]
    public void There_is_an_uninitialized_project_in_ide()
    {
        var emptyIde = new StubIdeScope(TestOutputHelper);
        var uninitializedProject = new InMemoryStubProjectScope(TestOutputHelper);

        var taggerProvider = CreateTaggerProvider(emptyIde);
        var command = new RenameStepCommand(
            emptyIde,
            new StubBufferTagAggregatorFactoryService(taggerProvider),
            taggerProvider);

        var inputText = new TestText(string.Empty);
        var textView = StubWpfTextView.CreateTextView(inputText, text =>
        {
            var textBuffer = VsxStubObjects.CreateTextBuffer(text.ToString(), "whatEver");
            textBuffer.Properties.AddProperty(typeof(IProjectScope), uninitializedProject);
            return textBuffer;
        });

        command.PreExec(textView, command.Targets.First());

        var stubLogger = GetStubLogger(emptyIde);

        stubLogger.Warnings().Messages
            .Should()
            .ContainSingle("Unable to find step definition usages: the project is not initialized yet.");
    }

    [Fact]
    public void Only_reqnroll_projects_are_supported()
    {
        MockableDiscoveryService.Setup(ProjectScope, TimeSpan.Zero);
        var taggerProvider = CreateTaggerProvider(ProjectScope.IdeScope);
        var command = new RenameStepCommand(ProjectScope.IdeScope,
            new StubBufferTagAggregatorFactoryService(taggerProvider),
            taggerProvider);
        var inputText = new TestText(string.Empty);
        var textView = CreateTextView(inputText, VsContentTypes.CSharp, "Steps.cs");

        command.PreExec(textView, command.Targets.First());

        WarningMessages().Should()
            .ContainSingle(
                "Unable to find step definition usages: the project is not detected to be a Reqnroll project.");
    }

    [Fact]
    public void Reqnroll_projects_must_have_feature_files()
    {
        MockableDiscoveryService.Setup(ProjectScope, TimeSpan.Zero);
        ProjectScope.AddReqnrollPackage();
        var taggerProvider = CreateTaggerProvider(ProjectScope.IdeScope);
        var command = new RenameStepCommand(ProjectScope.IdeScope,
            new StubBufferTagAggregatorFactoryService(taggerProvider),
            taggerProvider);
        var inputText = new TestText(string.Empty);

        var textView = CreateTextView(inputText, VsContentTypes.CSharp, "Steps.cs");
        command.PreExec(textView, command.Targets.First());

        WarningMessages().Should()
            .ContainSingle(
                "Unable to find step definition usages: could not find any Reqnroll project with feature files.");
    }

    [Fact]
    public async Task There_must_be_at_lest_one_step_definition()
    {
        await StepDefinitionMustHaveValidExpression(TestStepDefinition.Void,
            "No step definition found that is related to this position");
    }

    [Fact]
    public async Task StepDefinition_regex_must_be_valid()
    {
        var stepDefinition = ArrangeStepDefinition(string.Empty);
        stepDefinition.TestExpression = SyntaxFactory.MissingToken(SyntaxKind.StringLiteralToken);
        stepDefinition.Regex = default!;

        await StepDefinitionMustHaveValidExpression(stepDefinition,
            "Unable to rename step, the step definition expression cannot be detected.");
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(2, "")]
    [InlineData(3, @"""foo? (\d+) bar""")]
    [InlineData(4, @"""foo (?:\d+) bar""")]
    [InlineData(5, @"""foo [a-z] bar""")]
    [InlineData(6, @"""foo. (\d+) bar""")]
    [InlineData(7, @"""foo* (\d+) bar""")]
    [InlineData(8, @"""foo+ (\d+) bar""")]
    // [InlineData(9, @"""some \\(context\\)""")] -- represents `some \(context\)` that is a simple part (all op masked)
    [InlineData(10, @"""some \{context\}""")]
    [InlineData(11, @"""some \[context\]""")]
    [InlineData(12, @"""some \[context]""")]
    // [InlineData(13, @"""chars \\\\\\*\\+\\?\\|\\{\\}\\[\\]\\(\\)\\^\\$\\#""")] -- represents `chars \\\*\+\?\|\{\}\[\]\(\)\^\$\#` that is a simple part (all op masked)
    public async Task StepDefinition_expression_cannot_be_modified(int _, string emptyExpression)
    {
        var stepDefinition = ArrangeStepDefinition(emptyExpression);

        await StepDefinitionMustHaveValidExpression(stepDefinition,
            "The non-parameter parts cannot contain expression operators");
    }

    [Theory]
    [InlineData("\"\"")]
    public async Task StepDefinition_expression_must_be_valid(string invalidExpression)
    {
        var stepDefinition = ArrangeStepDefinition(invalidExpression);

        await StepDefinitionMustHaveValidExpression(stepDefinition, "Step definition expression is invalid");
    }

    [Fact]
    public async Task Constant_is_not_supported_in_step_definition_expression()
    {
        var stepDefinition = ArrangeStepDefinition("ConstantValue");
        stepDefinition.Regex = "^I press add$";

        await StepDefinitionMustHaveValidExpression(stepDefinition,
            "No expressions found to replace for [When(I press add)]: WhenIPressAdd");
    }

    private async Task StepDefinitionMustHaveValidExpression(TestStepDefinition stepDefinition, string errorMessage)
    {
        var featureFile = ArrangeOneFeatureFile(string.Empty);
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        Dump(textView, "Step definition class after rename");
        WarningMessages().Should().Contain(errorMessage);
    }

    [Theory]
    [InlineData("01", @"""I press add""", @"I choose add", @"        [When(""I choose add"")]")]
    [InlineData("02", @"""I press add""", @"I \""choose\"" add", @"        [When(@""I \""""choose\"""" add"")]")]
    [InlineData("03", @"""I \""press\"" add""", @"I choose add", @"        [When(""I choose add"")]")]
    [InlineData("04", @"""I \""press\"" add""", @"I \""choose\"" add", @"        [When(@""I \""""choose\"""" add"")]")]
    [InlineData("05", @"""\""I press add \""""", @"\""I choose add\""", @"        [When(@""\""""I choose add\"""""")]")]
    [InlineData("06", @"@""I press add""", @"I choose add", @"        [When(@""I choose add"")]")]
    [InlineData("07", @"@""I """"press"""" add""", @"I ""choose"" add", @"        [When(@""I """"choose"""" add"")]")]
    [InlineData("08", @"""I press (/d)""", @"I choose (/d)", @"        [When(""I choose (/d)"")]")]
    [InlineData("09", @"""I press (.*)""", @"I press (.*) button", @"        [When(""I press (.*) button"")]")]
    [InlineData("10", @"""(.*) press add""", @"(.*) press add button", @"        [When(""(.*) press add button"")]")]
    [InlineData("11", @"""(.*) press add""", @"On main screen (.*) press add",
        @"        [When(""On main screen (.*) press add"")]")]
    [InlineData("12", @"""(.*) press (.*)""", @"(.*) choose (.*)", @"        [When(""(.*) choose (.*)"")]")]
    [InlineData("13", @"""I press (.*)(.*)""", @"I press (.*) and (.*)", @"        [When(""I press (.*) and (.*)"")]")]
    [InlineData("14", @"""I press add""", @"I choose \(add\)", @"        [When(@""I choose \(add\)"")]")]
    [InlineData("15", @"@""I press add""", @"I choose \(add\)", @"        [When(@""I choose \(add\)"")]")]
    [InlineData("16", @"@""I press add""", @"I choose ""add""", @"        [When(@""I choose """"add"""""")]")]
    [InlineData("17", @"""I press add""", @"I choose ""add""", @"        [When(@""I choose """"add"""""")]")]
    [InlineData("18", @"""I press add (.*)""", @"I choose \(add\) (.*)", @"        [When(@""I choose \(add\) (.*)"")]")]
    public async Task Step_definition_class_has_one_matching_expression(string _, string originalExpression,
        string dialogExpression, string updatedLine)
    {
        var stepDefinitions = ArrangeStepDefinition(originalExpression);
        var featureFile = ArrangeOneFeatureFile(string.Empty);
        ArrangePopup(dialogExpression);
        var (textView, command) = await ArrangeSut(stepDefinitions, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        var testText = Dump(textView, "Step definition class after rename");
        testText.Lines[6].Should().Be(updatedLine);
        ThereWereNoWarnings();
        await BindingRegistryIsModified(dialogExpression);
    }

    [Theory]
    [InlineData("01", @"""I press add""", @"I press (.*)", "Parameter count mismatch")]
    [InlineData("02", @"""I press (.*)""", @"I press add", "Parameter count mismatch")]
    [InlineData("03", @"""I press add button""", @"I press (.*) button", "Parameter count mismatch")]
    [InlineData("04", @"""I press (.*) button""", @"I press add button", "Parameter count mismatch")]
    [InlineData("05", @"""I press add button""", @"(.*) press add button", "Parameter count mismatch")]
    [InlineData("06", @"""(.*) press add button""", @"I press add button", "Parameter count mismatch")]
    [InlineData("07", @"""(.*) press add""", @"(.*) press (.*)", "Parameter count mismatch")]
    [InlineData("08", @"""(.*) press (.*)""", @"(.*) press add", "Parameter count mismatch")]
    [InlineData("09", @"""I press (.*)""", @"I press (.*)(.*)", "Parameter count mismatch")]
    [InlineData("10", @"""I press (.*)""", @"I press (/d)", "Parameter expression mismatch")]
    [InlineData("11", @"""I press (.*)""", @"I press (/d)(.*)", "Parameter count mismatch",
        "Parameter expression mismatch")]
    [InlineData("13", @"""I press add""", @"I ( add (.*)", "Parameter count mismatch")]
    [InlineData("14", @"""I press (.*)""", @"I press add .*)",
        "The non-parameter parts cannot contain expression operators", "Parameter count mismatch")]
    [InlineData("15", @"""I press add""", @"I pr?ess add",
        "The non-parameter parts cannot contain expression operators")]
    [InlineData("16", @"""I press (.*)""", @"I pr?ess (.*)",
        "The non-parameter parts cannot contain expression operators")]
    public async Task User_cannot_type_invalid_expression(string _, string testExpression, string modelStepText,
        params string[] errorMessages)
    {
        var stepDefinitions = ArrangeStepDefinition(testExpression);
        var featureFile = ArrangeOneFeatureFile(string.Empty);
        ArrangePopup(modelStepText);
        var (textView, command) = await ArrangeSut(stepDefinitions, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        var logMessage = WarningMessages().Single();
        var actualErrorMessages = logMessage.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
        actualErrorMessages.Should().BeEquivalentTo(errorMessages);
    }

    [Fact]
    public async Task Step_definition_is_declared_with_a_derived_attribute()
    {
        var stepDefinition = ArrangeStepDefinition(@"""I press add""", "When", "WhenDerived");
        var featureFile = ArrangeOneFeatureFile(string.Empty);
        ArrangePopup(@"I choose add");
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        var testText = Dump(textView, "Step definition class after rename");
        testText.Lines[6].Should().Be(@"        [WhenDerived(""I choose add"")]");
        ThereWereNoWarnings();
    }

    [Fact]
    public async Task Popup_appears_when_there_are_multiple_step_definitions()
    {
        var stepDefinitions = ArrangeMultipleStepDefinitions();
        var featureFiles = new[] {ArrangeOneFeatureFile(string.Empty)};
        var (textView, command) = await ArrangeSut(stepDefinitions, featureFiles);

        command.PreExec(textView, command.Targets.First());

        var ideActions = ProjectScope.IdeScope.Actions as StubIdeActions;
        ideActions!.LastShowContextMenuHeader.Should().Be("Choose step definition to rename");
        ideActions.LastShowContextMenuItems.Select(item => item.Label)
            .Should().BeEquivalentTo(stepDefinitions.Select(sd => sd.PopupLabel));
        ThereWereNoWarnings();
    }

    [Theory]
    [InlineData(0, new[]
    {
        @"        [When(""I choose add"")]",
        @"        [Given(""I press add"")]",
        @"        [When(""I select add"")]"
    })]
    [InlineData(1, new[]
    {
        @"        [When(""I press add"")]",
        @"        [Given(""I choose add"")]",
        @"        [When(""I select add"")]"
    })]
    [InlineData(2, new[]
    {
        @"        [When(""I press add"")]",
        @"        [Given(""I press add"")]",
        @"        [When(""I choose add"")]"
    })]
    public async Task Selected_step_definition_is_renamed_when_there_are_multiple(int chosenOption,
        string[] expectedLines)
    {
        var stepDefinitions = ArrangeMultipleStepDefinitions();
        var featureFiles = new[] {ArrangeOneFeatureFile(string.Empty)};
        ArrangePopup(@"I choose add");
        var (textView, command) = await ArrangeSut(stepDefinitions, featureFiles);

        command.PreExec(textView, command.Targets.First());

        var ideActions = ProjectScope.IdeScope.Actions as StubIdeActions;
        var chosenItem = ideActions!.LastShowContextMenuItems[chosenOption];
        chosenItem.Command(chosenItem);

        await WaitForCommandToComplete(command);

        var testText = Dump(textView, "Step definition class after rename");
        testText.Lines[6].Should().Be(expectedLines[0]);
        testText.Lines[7].Should().Be(expectedLines[1]);
        testText.Lines[8].Should().Be(expectedLines[2]);
        ThereWereNoWarnings();
    }

    [Fact]
    public async Task Step_in_the_feature_file_is_renamed_simple_case()
    {
        var stepDefinition = ArrangeStepDefinition(@"""I press add""");
        var featureFile = ArrangeOneFeatureFile(@"Feature: Feature1
                Scenario: Scenario1
                    When I press add");
        ArrangePopup(@"I choose add");
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        ProjectScope.IdeScope.GetTextBuffer(new SourceLocation(featureFile.FileName, 1, 1),
            out var featureFileTextBuffer);
        var featureText = Dump(featureFileTextBuffer, "Feature file after rename");
        featureText.Lines[2].Should().Be(@"                    When I choose add");
        ThereWereNoWarnings();
    }

    [Theory]
    [InlineData(1, @"""I press add""", @"I choose add", @"I press add", @"I choose add")]
    [InlineData(2, @"""I press add""", @"I choose \(add\)", @"I press add", @"I choose (add)")]
    [InlineData(3, @"""I press add (.*)""", @"I choose \(add\) (.*)", @"I press add 42", @"I choose (add) 42")]
    public async Task Step_in_the_feature_file_is_renamed(int _, string originalExpression, string updatedExpression,
        string originalStepText, string expectedStepText)
    {
        var stepDefinition = ArrangeStepDefinition(originalExpression);
        var featureFile = ArrangeOneFeatureFile($@"Feature: Feature1
                Scenario: Scenario1
                    When {originalStepText}");
        ArrangePopup(updatedExpression);
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        var featureText = Dump(featureFile, "Feature file after rename");
        featureText.Lines[2].Should().Be($@"                    When {expectedStepText}");
        ThereWereNoWarnings();
    }

    [Theory]
    [InlineData("01", @"""I press add""", @"I choose add", @"I press add", @"I choose add")]
    [InlineData("02", @"""I press add""", @"I choose \(add\)", @"I press add", @"I choose (add)")]
    [InlineData("03", @"""I press add (.*)""", @"I choose \(add\) (.*)", @"I press add 42", @"I choose (add) 42")]
    public async Task Step_of_scenario_outline_in_the_feature_file_is_renamed(string _, string originalExpression,
        string updatedExpression, string originalStepText, string expectedStepText)
    {
        TestFeatureFile featureFile = ArrangeOneFeatureFile($@"Feature: Feature1
                Scenario: Scenario1
                    When {originalStepText}");

        var featureText = await OneFeatureFileRename(originalExpression, updatedExpression, featureFile);

        featureText.Lines[2].Should().Be($@"                    When {expectedStepText}");

        ThereWereNoWarnings();
    }

    [Theory]
    [InlineData("01", @"""I press add""", @"I choose add", @"I press <p1>",
        @"calculator.feature(3,21): Could not rename scenario outline step with placeholders: I press <p1>")]
    [InlineData("02", @"""I press add (.*)""", @"I choose \(add\) (.*)",
        @"I press <p1> 42",
        @"calculator.feature(3,21): Could not rename scenario outline step with placeholders: I press <p1> 42")]
    public async Task Step_of_scenario_outline_in_the_feature_cannot_be_renamed(string _,
        string originalExpression, string updatedExpression, string originalStepText,
        params string[] errorMessages)
    {
        TestFeatureFile featureFile = ArrangeOneFeatureFile($@"Feature: Feature1
                Scenario Outline: Scenario1
                    When {originalStepText}
                Examples:
                    |p1 |
                    |add|");

        await OneFeatureFileRename(originalExpression, updatedExpression, featureFile);

        WarningMessages().Should()
            .ContainSingle(ProjectScope.ProjectFolder + string.Join(Environment.NewLine, errorMessages));
    }

    private async Task<TestText> OneFeatureFileRename(string originalExpression, string updatedExpression,
        TestFeatureFile featureFile)
    {
        var stepDefinition = ArrangeStepDefinition(originalExpression);

        ArrangePopup(updatedExpression);
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        return Dump(featureFile, "Feature file after rename");
    }

    [Theory]
    [InlineData(1, @"""I press add""", @"I press add")]
    [InlineData(2, @"@""I press add""", @"I press add")]
    [InlineData(3, @"""I press \\(add\\)""", @"I press \(add\)")]
    [InlineData(4, @"@""I press \(add\)""", @"I press \(add\)")]
    public async Task The_right_expression_is_loaded_to_the_dialog(int _, string originalCSharpExpression,
        string expectedExpression)
    {
        var stepDefinition = ArrangeStepDefinition(originalCSharpExpression);
        var featureFile = ArrangeOneFeatureFile(string.Empty);
        RenameStepViewModel? viewModel = null;
        (ProjectScope.IdeScope.WindowManager as StubWindowManager)?
            .RegisterWindowAction<RenameStepViewModel>(model => viewModel = model);
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        command.PreExec(textView, command.Targets.First());

        viewModel?.StepText.Should().Be(expectedExpression);
        ThereWereNoWarnings();
    }
}
