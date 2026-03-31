using ReqnrollConnector;
using ReqnrollConnector.AssemblyLoading;
using ReqnrollConnector.Logging;

var log = new ConsoleLogger();


return (int)new Runner(log).Run(args, new TestAssemblyContextFactory());
