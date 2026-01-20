namespace ReqnrollConnector.CommandLineOptions;

public record ConnectorOptions(bool DebugMode)
{
    public static ConnectorOptions Parse(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Command is missing!");

        var commandArgsList = args.Skip(1).ToList();
        int debugArgIndex = commandArgsList.IndexOf("--debug");
        var debugMode = false;
        if (debugArgIndex >= 0)
        {
            debugMode = true;
            commandArgsList.RemoveAt(debugArgIndex);
        }

        var commandName = args[0];
        var commandArgs = commandArgsList.ToArray();

        return commandName switch
        {
            DiscoveryCommand.CommandName => DiscoveryOptions.Parse(commandArgs, debugMode),
            _ => throw new ArgumentException($"Invalid command: {commandName}")
        };
    }
}
