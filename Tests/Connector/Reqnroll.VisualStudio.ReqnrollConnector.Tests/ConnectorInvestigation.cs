#pragma warning disable xUnit1008
namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("ApprovalTestData")]
public class ConnectorInvestigation : ApprovalTestBase
{
    public ConnectorInvestigation(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    /*
    This is not a real test, but allows us to investigate connector related issues. 
    Usage: 
        1 - create a json file in "<local repo>\Reqnroll.VS\Tests\Reqnroll.VisualStudio.ReqnrollConnector.Tests\ApprovalTestData\ConnectorInvestigation\" folder
        2 - add the file name as inline data
        3 - unskip the Theory attribute to let te xUnit discover
        4 - run or debug the test
    There is a sample "Reqnroll.VisualStudio.Specs.json" file in the folder which points to the solution's tests
    */
    //[Theory]
    [Theory(Skip = "Use for investigation")]
    //[InlineData("Reqnroll.VisualStudio.Specs")]
    [InlineData("Microsoft.AspNetCore.Mvc.Core")]
    public void Approval(string testName)
    {
        //arrange
        var testData = ArrangeTestData<RunnerTestData>(testName);
#if DEBUG
        var targetFolder = testData.TargetFolder.Replace("$(BuildConfiguration)", "Debug");
#else
        var targetFolder = testData.TargetFolder.Replace("$(BuildConfiguration)", "Release");
#endif
        targetFolder =
            targetFolder.Replace("$(ApprovalTestData)", "..\\..\\..\\ApprovalTestData\\ConnectorInvestigation");
        testData = testData with {TargetFolder = targetFolder};

        //act
        var result = Invoke(testData.TargetFolder, testData.TestAssembly, testData.ConfigFile);

        //assert
        Assert(result, testData.TargetFolder);
    }

    private record RunnerTestData(string TargetFolder, string TestAssembly, string? ConfigFile);
}
