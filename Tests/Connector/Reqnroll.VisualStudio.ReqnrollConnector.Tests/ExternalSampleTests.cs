namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

/// <summary>
/// This test class validates whether connector can work with various sample projects from external repositories.
/// It clones/pulls the repository to a temp folder, builds each Reqnroll project, and runs discovery using the connector.
/// </summary>
public class ExternalSampleTests : SampleProjectTestBase
{
    public ExternalSampleTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory]
    [MemberData(nameof(GetProjectsForRepository), "https://github.com/reqnroll/Sample-ReqOverflow", "")]
    public void ReqOverflow(string testCase, string projectFile, string repositoryDirectory)
    {
        ValidateProject(testCase, projectFile, repositoryDirectory);
    }

    private const string IgnoredExploratoryTestProjects =
        "SpecFlowCompatibilityProject.Net472;CleanReqnrollProject.Net481.x86;VsExtConnectorTestSamples";

    [Theory]
    [MemberData(nameof(GetProjectsForRepository), "https://github.com/reqnroll/Reqnroll.ExploratoryTestProjects", $"BigReqnrollProject;SpecFlowProject;OldProjectFileFormat.Empty;ReqnrollFormatters.CustomizedHtml;CustomPlugins.TagDecoratorGeneratorPlugin;{IgnoredExploratoryTestProjects}")]
    public void ExploratoryTestProjects(string testCase, string projectFile, string repositoryDirectory)
    {
        ValidateProject(testCase, projectFile, repositoryDirectory);
    }

    // example of running tests for a local repository (the path is relative to the solution root or absolute)
    //[Theory]
    //[MemberData(nameof(GetProjectsForRepository), @"..\Reqnroll.ExploratoryTestProjects\VsExtConnectorTestSamples", "")]
    //public void LocalExploratoryTestProjects(string testCase, string projectFile, string repositoryDirectory)
    //{
    //    ValidateProject(testCase, projectFile, repositoryDirectory);
    //}
}
