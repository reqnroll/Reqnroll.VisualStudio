using System;
using CommandLine;
using CommandLineParser = CommandLine.Parser;

namespace Reqnroll.SampleProjectGenerator;

internal class Program
{
    private static int Main(string[] args)
    {
        try
        {
            CommandLineParser.Default.ParseArguments<GeneratorOptions>(args)
                .WithParsed(RunOptionsAndReturnExitCode)
                .WithNotParsed(HandleParseError);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }

    private static void RunOptionsAndReturnExitCode(GeneratorOptions opts)
    {
        var generator = opts.CreateProjectGenerator(Console.WriteLine);
        generator.Generate();
    }

    private static void HandleParseError(IEnumerable<Error> errs)
    {
        Console.WriteLine("Errors");
        Console.WriteLine(string.Join(Environment.NewLine, errs));
    }
}
