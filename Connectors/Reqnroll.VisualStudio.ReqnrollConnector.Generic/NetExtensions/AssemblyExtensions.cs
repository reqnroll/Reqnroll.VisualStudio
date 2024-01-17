// ReSharper disable once CheckNamespace

namespace System.Reflection;

public static class AssemblyExtensions
{
    public static FileDetails GetLocation(this Assembly assembly) =>
        FileDetails.FromPath(
            assembly.Location
        );
}
