using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Utils;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

[UseReporter /*(typeof(VisualStudioReporter))*/]
[UseApprovalSubdirectory("ApprovalTestData\\ConnectorOptionsParse")]
public class ConnectorOptionsParseTests
{
    public static IEnumerable<LabeledTestData<(string[] args, string expected)>> TestCases =
        new LabeledTestData<(string[] args, string expected)>[]
        {
            new("discovery assembly",
                (new[] {"discovery", "../targetAssembly.dll"},
                    @"DiscoveryOptions { DebugMode = False, AssemblyFile = <<targetPath>>\targetAssembly.dll, ConfigFile = , ConnectorFolder = <<connectorPath>> }")),
            new("discovery assembly with configuration",
                (new[] {"discovery", "../targetAssembly.dll", "../configuration.json"},
                    @"DiscoveryOptions { DebugMode = False, AssemblyFile = <<targetPath>>\targetAssembly.dll, ConfigFile = <<targetPath>>\configuration.json, ConnectorFolder = <<connectorPath>> }")),
            new("Missing arguments",
                (Array.Empty<string>(),
                    @"ArgumentException: Command is missing!")),
            new("Invalid command",
                (new[] {"xxx"},
                    @"ArgumentException: Invalid command: xxx")),
            new("debug",
                (new[] {"discovery", "--debug", "../targetAssembly.dll"},
                    @"DiscoveryOptions { DebugMode = True, AssemblyFile = <<targetPath>>\targetAssembly.dll, ConfigFile = , ConnectorFolder = <<connectorPath>> }")),
            new("debug must be after command",
                (new[] {"--debug", "yyy"},
                    "ArgumentException: Invalid command: --debug")),
            new("discovery command without arguments",
                (new[] {"discovery"},
                    "InvalidOperationException: Usage: discovery <test-assembly-path> [<configuration-file-path>]"))
        };

    private readonly ITestOutputHelper _testOutputHelper;

    public ConnectorOptionsParseTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [LabeledMemberData(nameof(TestCases))]
    public void Approval(LabeledTestData<(string[] args, string expected)> @case)
    {
        //arrange
        NamerFactory.AdditionalInformation = @case.Label.Replace(' ', '_');

        //act
        string resultAsString;
        try
        {
            var result = ConnectorOptions.Parse(@case.Data.args);
            resultAsString = result.ToString();
        }
        catch (Exception e)
        {
            resultAsString = e.GetType().Name +": "+ e.Message;
        }

        //assert
        _testOutputHelper.WriteLine("---------------------------result- ----------------------------------");
        _testOutputHelper.WriteLine(resultAsString);
        var connectorPath = FileDetails.FromPath("this").DirectoryName ?? "?";
        var targetPath = FileDetails.FromPath("../this").DirectoryName ?? "?";
        resultAsString = resultAsString.Replace(connectorPath, "<<connectorPath>>");
        resultAsString = resultAsString.Replace(targetPath, "<<targetPath>>");
        _testOutputHelper.WriteLine("--------------------------scrubbed-----------------------------------");
        _testOutputHelper.WriteLine(resultAsString);
        _testOutputHelper.WriteLine("--------------------------expected-----------------------------------");
        _testOutputHelper.WriteLine(@case.Data.expected);
        Approvals.AssertText(@case.Data.expected, resultAsString);
    }
}
