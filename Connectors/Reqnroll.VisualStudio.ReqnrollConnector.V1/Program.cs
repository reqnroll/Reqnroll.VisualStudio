using System;
using Reqnroll.VisualStudio.ReqnrollConnector.AppDomainHelper;

namespace Reqnroll.VisualStudio.ReqnrollConnector;

internal class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        using (AssemblyHelper.SubscribeResolveForAssembly(typeof(Program)))
        {
            return new ConsoleRunner().EntryPoint(args);
        }
    }
}
