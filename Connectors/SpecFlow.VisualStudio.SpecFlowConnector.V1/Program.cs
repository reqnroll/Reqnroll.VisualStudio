using System;
using SpecFlow.VisualStudio.SpecFlowConnector.AppDomainHelper;

namespace SpecFlow.VisualStudio.SpecFlowConnector;

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
