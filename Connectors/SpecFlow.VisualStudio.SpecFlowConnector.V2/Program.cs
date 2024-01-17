using System;

namespace SpecFlow.VisualStudio.SpecFlowConnector;

internal class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        return new ConsoleRunner().EntryPoint(args);
    }
}
