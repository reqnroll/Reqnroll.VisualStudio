#nullable disable

namespace SpecFlow.VisualStudio.SpecFlowConnector;

public class ConnectorOptions
{
    public string Command { get; set; }
    public bool DebugMode { get; set; }

    public static ConnectorOptions Parse(string[] args, out string[] commandArgs)
    {
        if (args == null || args.Length == 0)
            throw new ArgumentException("Command is missing!");

        var options = new ConnectorOptions
        {
            Command = args[0]
        };

        var commandArgsList = args.Skip(1).ToList();
        int debugArgIndex = commandArgsList.IndexOf("--debug");
        if (debugArgIndex >= 0)
        {
            options.DebugMode = true;
            commandArgsList.RemoveAt(debugArgIndex);
        }

        commandArgs = commandArgsList.ToArray();
        return options;
    }
}
