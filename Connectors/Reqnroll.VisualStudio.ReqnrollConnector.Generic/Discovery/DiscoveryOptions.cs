namespace ReqnrollConnector.Discovery;

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
        var pathTuple = (testAssemblyFile, configFile);
        var validatedPaths = ValidateTargetFolder(pathTuple);
        var (targetAssemblyFile, configFileOption) = validatedPaths;
        
        var assemblyLocation = typeof(Runner).Assembly.GetLocation();
        var connectorDir = Directory(assemblyLocation);
        
        string? configFileFullName = null;
        if (configFileOption is Some<FileDetails> someConfigFile)
        {
            configFileFullName = someConfigFile.Content.FullName;
        }
        
        return new DiscoveryOptions(
            debugMode,
            targetAssemblyFile.FullName,
            configFileFullName,
            connectorDir.FullName
        ) as ConnectorOptions;
    }

    private static string[] ValidateParameterCount(string[] args) => args.Length >= 1
        ? args
        : throw new InvalidOperationException("Usage: discovery <test-assembly-path> [<configuration-file-path>]");

    private static FileDetails AssemblyPath(string[] args) => FileDetails.FromPath(args[0]);

    private static Option<FileDetails> ConfigPath(string[] args) =>
        args.Length < 2 || string.IsNullOrWhiteSpace(args[1])
            ? None.Value
            : FileDetails.FromPath(args[1]);

    private static (FileDetails targetAssemblyFile, Option<FileDetails> configFile)
        ValidateTargetFolder((FileDetails, Option<FileDetails> ) x)
    {
        var (targetAssemblyFile, configFile) = x;
        if (targetAssemblyFile.Directory is Some<DirectoryInfo>) return x;
        throw new InvalidOperationException(
            $"Unable to detect target folder from test assembly path '{targetAssemblyFile}'");
    }

    private static DirectoryInfo Directory(FileDetails fileDetails)
    {
        if (fileDetails.Directory is Some<DirectoryInfo> someDir)
        {
            return someDir.Content;
        }
        throw new InvalidOperationException("Unable to detect connector folder.");
    }
}
