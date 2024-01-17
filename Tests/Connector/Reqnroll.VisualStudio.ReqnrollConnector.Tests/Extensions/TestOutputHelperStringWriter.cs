namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests.Extensions;

public class TestOutputHelperStringWriter : StringWriter
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TestOutputHelperStringWriter(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public override void WriteLine(string? value)
    {
        _testOutputHelper.WriteLine(value);
    }
}
