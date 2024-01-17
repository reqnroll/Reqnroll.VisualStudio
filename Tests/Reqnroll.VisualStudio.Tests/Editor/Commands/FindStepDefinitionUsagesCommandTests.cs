#pragma warning disable xUnit1026 //Theory method 'xxx' does not use parameter '_'

namespace Reqnroll.VisualStudio.Tests.Editor.Commands;

public class FindStepDefinitionUsagesCommandTests : CommandTestBase<FindStepDefinitionUsagesCommand>
{
    public FindStepDefinitionUsagesCommandTests(ITestOutputHelper testOutputHelper) :
        base(testOutputHelper,
            (ps, tp) => new FindStepDefinitionUsagesCommand(ps.IdeScope,
                new StubBufferTagAggregatorFactoryService(tp), tp), "???")
    {
    }

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
    }

    [Fact]
    public async Task Could_not_find_any_usage()
    {
        var stepDefinition = ArrangeStepDefinition(@"""I choose add""");
        var featureFile = ArrangeOneFeatureFile();
        var (textView, command) = await ArrangeSut(stepDefinition, featureFile);

        await InvokeAndWaitAnalyticsEvent(command, textView);

        (ProjectScope.IdeScope.Actions as StubIdeActions)!.LastShowContextMenuItems.Should()
            .Contain(mi => mi.Label == "Could not find any usage");
    }

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
    }
}
