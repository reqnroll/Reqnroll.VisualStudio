#pragma warning disable xUnit1026 //Theory method 'xxx' does not use parameter '_'

namespace Reqnroll.VisualStudio.Tests.Editor.Commands;

public class FindUnusedStepDefinitionsCommandTests : CommandTestBase<FindUnusedStepDefinitionsCommand>
{
    public FindUnusedStepDefinitionsCommandTests(ITestOutputHelper testOutputHelper) :
        base(testOutputHelper,
            (ps, tp) => new FindUnusedStepDefinitionsCommand(ps.IdeScope,
                new StubBufferTagAggregatorFactoryService(tp), tp), "???")
    {
    }

    [Fact]
    public async Task Can_find_StepDefinition_Not_Used()
    {
        var stepDefinition = ArrangeStepDefinition(@"""I choose add""");
        var featureFile = ArrangeOneFeatureFile();
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        (ProjectScope.IdeScope.Actions as StubIdeActions)!.LastShowContextMenuItems.Should()
            .Contain(mi => mi.Label == @"Steps.cs(9,9): [When(""I choose add"")] WhenIPressAdd");
    }

    [Fact]
    public async Task Can_find_a_StepDefinition_With_Multiple_Step_Attributes_Not_Used()
    {
        var stepDefinition = ArrangeStepDefinition(@"""I choose add""");
        var stepDefinition2 = ArrangeStepDefinition(@"""I select add""");
        var stepDefs = new[] { stepDefinition, stepDefinition2 };
        var featureFile = ArrangeOneFeatureFile();
        var (textView, command) = await ArrangeSut(stepDefs, new[] { featureFile });

        await InvokeAndWaitAnalyticsEvent(command, textView);

        (ProjectScope.IdeScope.Actions as StubIdeActions)!.LastShowContextMenuItems.Should()
            .HaveCount(2).And
            .Contain(mi => mi.Label == "Steps.cs(10,9): [When(\"I choose add\")] WhenIPressAdd");
    }


    [Fact]
    public async Task Cannot_find_an_unused_StepDefinition_With_StepDefinition_Attribute()
    {
        var stepDefinition = ArrangeStepDefinition(@"""I press add""", "StepDefinition");
        var featureFile = ArrangeOneFeatureFile();
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        (ProjectScope.IdeScope.Actions as StubIdeActions)!.LastShowContextMenuItems.Should()
            .HaveCount(1).And
            .Contain(mi => mi.Label == "There are no unused step definitions");
    }
}
