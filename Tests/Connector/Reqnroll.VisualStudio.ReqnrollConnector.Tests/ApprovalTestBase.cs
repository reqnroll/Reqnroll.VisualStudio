using Reqnroll.VisualStudio.ReqnrollConnector.Models;
using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Utils;
using DiscoveryResult = Reqnroll.VisualStudio.ReqnrollConnector.Models.DiscoveryResult;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

public class ApprovalTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ApprovalTestBase(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    protected ProcessResult Invoke(string targetFolder, string testAssemblyFileName, string? configFileName)
    {
        var targetAssemblyFile =
            FileDetails.FromPath(targetFolder, testAssemblyFileName);

        var connectorFile = typeof(DiscoveryExecutor).Assembly.GetLocation();
        var outputFolder = Path.GetDirectoryName(targetAssemblyFile)!;

        FileDetails? configFile = null;
        if (configFileName != null)
        {
            configFile = FileDetails.FromPath(outputFolder, configFileName);
        }

        var directoryName = targetAssemblyFile.DirectoryName ?? "Unknown directory";

        var psiEx = new ProcessStartInfoEx(
            directoryName,
            connectorFile,
            string.Empty
        );
#if NETCOREAPP
        psiEx = psiEx with
        {
            ExecutablePath = "dotnet",
            Arguments = $"{connectorFile} "
        };
#endif
        var configFileArg = configFile?.FullName ?? string.Empty;

        psiEx = psiEx with
        {
            Arguments = psiEx.Arguments +
                        $"{ConnectorOptions.DiscoveryCommandName} {targetAssemblyFile} {configFileArg}"
        };

        _testOutputHelper.WriteLine($"{psiEx.ExecutablePath} {psiEx.Arguments}");

        ProcessResult result = Debugger.IsAttached
            ? InvokeInMemory(psiEx)
            : InvokeAsProcess(psiEx);
        return result;
    }

    private static ProcessResult InvokeInMemory(ProcessStartInfoEx psiEx)
    {
#if NETCOREAPP
        var split = psiEx.Arguments.Split(' ', 2);
        psiEx = psiEx with
        {
            ExecutablePath = split[0],
            Arguments = split[1]
        };
#endif
        Assembly? testAssembly = null;

        var logger = new TestConsoleLogger();
        var consoleRunner = new Runner(logger);
        var mockFileSystem = new FileSystem();
        var resultCode = consoleRunner.Run(
            psiEx.Arguments.Split(' '),
            (ctx, path) => testAssembly ??= ctx.LoadFromAssemblyPath(path));
        var result = new ProcessResult((int)resultCode, logger[LogLevel.Info], logger[LogLevel.Error], TimeSpan.Zero);
        return result;
    }

    private ProcessResult InvokeAsProcess(ProcessStartInfoEx psiEx)
    {
        var result = new ProcessHelper()
            .RunProcess(psiEx);
        return result;
    }

    protected void Assert(ProcessResult result, string targetFolder)
    {
        Assert(result, targetFolder, s => s);
    }

    protected void Assert(ProcessResult result, string targetFolder, Func<string, string> scrubber)
    {
        var rawContent = new StringBuilder()
            .Append((string?) $"stdout:{result.StdOutput}")
            .AppendLine((string?) $"stderr:{result.StdError}")
            .AppendLine($"resultCode:{result.ExitCode}")
            .Append($"time:{result.ExecutionTime}")
            .ToString();

        var scrubbed = TargetFolderScrubber(rawContent, targetFolder);
        scrubbed = scrubbed.Replace(typeof(DiscoveryExecutor).Assembly.ToString(), "<connector>");
        scrubbed = Regex.Replace(scrubbed, "errorMessage\": \".+\"", "errorMessage\": \"<errorMessage>\"");
        scrubbed = Regex.Replace(scrubbed, "(.*\r\n)*>>>>>>>>>>\r\n", "");
        scrubbed = Regex.Replace(scrubbed, "<<<<<<<<<<(.*[\r\n])*.*", "");
        scrubbed = XunitExtensions.StackTraceScrubber(scrubbed);
        scrubbed = ScrubVolatileParts(scrubbed);
        scrubbed = scrubber(scrubbed);

        _testOutputHelper.ApprovalsVerify(scrubbed);
    }

    private static string TargetFolderScrubber(string content, string targetFolder) =>
        content
            .Replace(targetFolder, "<<targetFolder>>")
            .Replace(targetFolder.Replace("\\", "\\\\"), "<<targetFolder>>");

    protected static T ArrangeTestData<T>(string testName)
    {
        var namer = new ShortenedUnitTestFrameworkNamer();
        NamerFactory.AdditionalInformation = testName;
        Approvals.RegisterDefaultNamerCreation(() => namer);

        var testDataFile = FileDetails.FromPath(namer.SourcePath, testName + ".json");

        var content = File.ReadAllText(testDataFile);
        var testData = JsonSerializer.Deserialize<T>(content);
        Debug.Assert(testData != null, nameof(testData) + " != null");
        return testData;
    }

    private static string ScrubVolatileParts(string content)
    {
        var deserialized = DeserializeObject<DiscoveryResult>(content);
        if (deserialized != null)
        {
            deserialized.StepDefinitions = Array.Empty<StepDefinition>();
            deserialized.SourceFiles = new Dictionary<string, string>();
            deserialized.TypeNames = new Dictionary<string, string>();
            return JsonSerialization.SerializeObjectCamelCase(deserialized);
        }

        return $"Cannot deserialize:{content}";
    }


    public static TResult? DeserializeObject<TResult>(string json, ILogger? log = null)
    {
        try
        {
            var deserializeObject = JsonSerializer.Deserialize<TResult>(json, JsonSerialization.GetJsonSerializerSettingsCamelCase());
            return deserializeObject;
        }
        catch (Exception e)
        {
            log?.Error(e.ToString());
            return default;
        }
    }

}
