namespace ReqnrollConnector.Utils;

public static class AssemblyExtensions
{
    public static FileDetails GetLocation(this Assembly assembly) =>
        FileDetails.FromPath(
            assembly.Location
        );
}
