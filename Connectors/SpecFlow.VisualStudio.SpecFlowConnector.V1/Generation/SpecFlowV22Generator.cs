namespace SpecFlow.VisualStudio.SpecFlowConnector.Generation;

public class SpecFlowV22Generator : SpecFlowVLatestGenerator
{
    protected override ITestGenerator CreateGenerator(ITestGeneratorFactory testGeneratorFactory,
        ProjectSettings projectSettings) =>
        testGeneratorFactory.ReflectionCallMethod<ITestGenerator>(nameof(ITestGeneratorFactory.CreateGenerator),
            projectSettings);
}
