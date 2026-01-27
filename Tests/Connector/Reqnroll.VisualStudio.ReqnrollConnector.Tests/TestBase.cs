using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Utils;

namespace Reqnroll.VisualStudio.ReqnrollConnector.Tests;

public class TestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TestBase(ITestOutputHelper testOutputHelper)
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
