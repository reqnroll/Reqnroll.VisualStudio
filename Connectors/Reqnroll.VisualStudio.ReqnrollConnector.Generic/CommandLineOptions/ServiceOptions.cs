namespace ReqnrollConnector.CommandLineOptions;

public record ServiceOptions(
    bool DebugMode,
    string ControlPipeName,
    string AssemblyFile,
    string? ConfigFile
) : ConnectorOptions(DebugMode)
{
    public static ConnectorOptions Parse(string[] args, bool debugMode)
    {
        string? controlPipe = null;
        string? assembly = null;
        string? config = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--control-pipe" when i + 1 < args.Length:
                    controlPipe = args[++i];
                    break;
                case "--assembly" when i + 1 < args.Length:
                    assembly = args[++i];
                    break;
                case "--config" when i + 1 < args.Length:
                    config = args[++i];
                    break;
            }
        }

        if (string.IsNullOrEmpty(controlPipe))
            throw new ArgumentException("--control-pipe is required for the service command");

        if (string.IsNullOrEmpty(assembly))
            throw new ArgumentException("--assembly is required for the service command");

        return new ServiceOptions(debugMode, controlPipe!, assembly!, config);
    }

    public override string ToString() =>
        $"ServiceOptions: ControlPipe={ControlPipeName}, Assembly={AssemblyFile}, Config={ConfigFile ?? "(none)"}, Debug={DebugMode}";
}
