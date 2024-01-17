namespace Reqnroll.VisualStudio.ReqnrollConnector.Generation;

public class ReqnrollV22Generator : ReqnrollVLatestGenerator
{
    protected override ITestGenerator CreateGenerator(ITestGeneratorFactory testGeneratorFactory,
        ProjectSettings projectSettings) =>
        testGeneratorFactory.ReflectionCallMethod<ITestGenerator>(nameof(ITestGeneratorFactory.CreateGenerator),
            projectSettings);
}
