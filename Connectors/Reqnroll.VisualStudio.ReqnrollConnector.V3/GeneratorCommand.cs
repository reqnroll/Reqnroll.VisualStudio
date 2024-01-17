using System;

namespace Reqnroll.VisualStudio.ReqnrollConnector;

public static class GeneratorCommand
{
    public const string CommandName = "generate";

    public static string Execute(string[] commandArgs) =>
        throw new NotSupportedException(
            "Design-time feature file code behind generation is not supported by SpecFlow v3 or later.");
}
