#pragma warning disable xUnit1026 //Theory method 'xxx' does not use parameter '_'

namespace Reqnroll.VisualStudio.Tests.Editor.Commands;

public class DefineStepsCommandTests : CommandTestBase<DefineStepsCommand>
{
    public DefineStepsCommandTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper, (ps, tp) =>
                new DefineStepsCommand(ps.IdeScope, new StubBufferTagAggregatorFactoryService(tp), tp),
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
            .ContainSingle("All steps have been defined in this file already.");
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
}
