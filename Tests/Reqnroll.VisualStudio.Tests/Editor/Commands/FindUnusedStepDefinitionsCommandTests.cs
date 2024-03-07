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
/*
    [Fact]
    public async Task Find_usages_in_a_modified_feature_file_too()
    {
        var stepDefinition = ArrangeStepDefinition(@"""I choose add""");
        TestFeatureFile featureFile = ArrangeOneFeatureFile();
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        ModifyFeatureFileInEditor(featureFile, new Span(50, 16), "When I choose add");
        Dump(featureFile, "After modification");
        await InvokeAndWaitAnalyticsEvent(command, textView);

        (ProjectScope.IdeScope.Actions as StubIdeActions)!.LastShowContextMenuItems.Should()
            .Contain(mi => mi.Label == "calculator.feature(3,8): When I choose add");
    }*/

    [Fact]
    public async Task Can_find_StepDefinition_Not_Used()
    {
        var stepDefinition = ArrangeStepDefinition(@"""I choose add""");
        var featureFile = ArrangeOneFeatureFile();
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        (ProjectScope.IdeScope.Actions as StubIdeActions)!.LastShowContextMenuItems.Should()
            .Contain(mi => mi.Label == "Steps.cs(9,9): WhenIPressAdd");
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
            .Contain(mi => mi.Label == "Steps.cs(10,9): WhenIPressAdd");
    }
    /*
        [Theory]
        [InlineData("01", @"""I press add""")]
        [InlineData("02", @"""I press (.*)""")]
        [InlineData("03", @"""I (.*) add""")]
        public async Task Find_usages(string _, string expression)
        {
            var stepDefinition = ArrangeStepDefinition(expression);
            var featureFile = ArrangeOneFeatureFile();
            var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

            await InvokeAndWaitAnalyticsEvent(command, textView);

            (ProjectScope.IdeScope.Actions as StubIdeActions)!.LastShowContextMenuItems.Should()
                .Contain(mi => mi.Label == "calculator.feature(3,8): When I press add");
        }*/
}
