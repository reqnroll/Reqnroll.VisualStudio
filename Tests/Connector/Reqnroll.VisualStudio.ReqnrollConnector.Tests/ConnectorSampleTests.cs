namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

public class ConnectorSampleTests : SampleProjectTestBase
{
    public ConnectorSampleTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory]
    [MemberData(nameof(GetProjectsForRepository), @"Tests\VsExtConnectorTestSamples", "")]
    public void All(string testCase, string projectFile, string repositoryDirectory)
    {
        ValidateProject(testCase, projectFile, repositoryDirectory);
    }
}