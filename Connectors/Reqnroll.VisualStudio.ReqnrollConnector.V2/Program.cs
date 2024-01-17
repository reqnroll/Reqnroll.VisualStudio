using System;

namespace Reqnroll.VisualStudio.ReqnrollConnector;

internal class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        return new ConsoleRunner().EntryPoint(args);
    }
}
