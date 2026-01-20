using ReqnrollConnector.Utils;

namespace ReqnrollConnector.CommandLineOptions;

public record DiscoveryOptions(
    bool DebugMode,
    string AssemblyFile,
    string? ConfigFile,
    string ConnectorFolder
) : ConnectorOptions(DebugMode)
{
    public static ConnectorOptions Parse(string[] args, bool debugMode)
    {
        var validatedArgs = ValidateParameterCount(args);
        var testAssemblyFile = AssemblyPath(validatedArgs);
        var configFile = ConfigPath(validatedArgs);
        ValidateTargetFolder(testAssemblyFile, configFile);
        
        var assemblyLocation = typeof(Runner).Assembly.GetLocation();
        var connectorDir = Directory(assemblyLocation);
        
        string? configFileFullName = null;
        if (configFile != null)
        {
            configFileFullName = configFile.FullName;
        }
        
        return new DiscoveryOptions(
            debugMode,
            testAssemblyFile.FullName,
            configFileFullName,
            connectorDir.FullName
        );
    }

    private static string[] ValidateParameterCount(string[] args) => args.Length >= 1
        ? args
        : throw new InvalidOperationException("Usage: discovery <test-assembly-path> [<configuration-file-path>]");

    private static FileDetails AssemblyPath(string[] args) => FileDetails.FromPath(args[0]);

    private static FileDetails? ConfigPath(string[] args) =>
        args.Length < 2 || string.IsNullOrWhiteSpace(args[1])
            ? null
            : FileDetails.FromPath(args[1]);

    private static void ValidateTargetFolder(FileDetails targetAssemblyFile, FileDetails? configFile)
    {
        if (targetAssemblyFile.Directory == null)
            throw new InvalidOperationException(
                $"Unable to detect target folder from test assembly path '{targetAssemblyFile}'");
    }

    private static DirectoryInfo Directory(FileDetails fileDetails)
    {
        if (fileDetails.Directory != null)
            return fileDetails.Directory;
        throw new InvalidOperationException("Unable to detect connector folder.");
    }
}
