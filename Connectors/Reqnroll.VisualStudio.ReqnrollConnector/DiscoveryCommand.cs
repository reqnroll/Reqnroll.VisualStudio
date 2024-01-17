using System;
using Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

namespace Reqnroll.VisualStudio.ReqnrollConnector;

public static class DiscoveryCommand
{
    public const string CommandName = "discovery";

    public static string Execute(string[] commandArgs)
    {
        var options = DiscoveryOptions.Parse(commandArgs);
        var processor = new DiscoveryProcessor(options);
        return processor.Process();
    }
}
