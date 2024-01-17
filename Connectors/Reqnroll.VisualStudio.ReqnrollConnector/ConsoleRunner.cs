#nullable disable

namespace Reqnroll.VisualStudio.ReqnrollConnector;

public class ConsoleRunner
{
    public int EntryPoint(string[] args)
    {
        try
        {
            string result = null;

            var connectorOptions = ConnectorOptions.Parse(args, out var commandArgs);

            if (connectorOptions.DebugMode && !Debugger.IsAttached)
                Debugger.Launch();

            switch (connectorOptions.Command)
            {
                case DiscoveryCommand.CommandName:
                {
                    result = DiscoveryCommand.Execute(commandArgs);
                    break;
                }
                case GeneratorCommand.CommandName:
                {
                    result = GeneratorCommand.Execute(commandArgs);
                    break;
                }
                default:
                    throw new ArgumentException($"Invalid command: {connectorOptions.Command}");
            }

            if (result == null)
                return 1;

            Console.WriteLine(JsonSerialization.MarkResult(result));

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {GetExceptionMessage(ex)}");
            Console.Error.WriteLine($"Exception: {GetExceptionTypes(ex)}");
            Console.Error.WriteLine("StackTrace:");
            Console.Error.WriteLine(GetStackTrace(ex));

            return ex is ArgumentException ? 3 : 4;
        }
    }

    public static string GetExceptionTypes(Exception ex)
    {
        var exceptionTypes = ex.GetType().FullName;
        if (ex.InnerException != null)
            exceptionTypes = $"{exceptionTypes}->{GetExceptionTypes(ex.InnerException)}";
        return exceptionTypes;
    }

    private static string GetExceptionMessage(Exception ex)
    {
        var message = ex.Message;
        if (ex.InnerException != null)
            message = $"{message} -> {GetExceptionMessage(ex.InnerException)}";
        return message;
    }

    private static string GetStackTrace(Exception ex)
    {
        var stackTrace = "";
        while (ex != null)
        {
            stackTrace = ex.StackTrace + Environment.NewLine + stackTrace;
            ex = ex.InnerException;
        }

        return stackTrace;
    }
}
