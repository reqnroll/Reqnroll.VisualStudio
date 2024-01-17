namespace SpecFlow.VisualStudio.Common;

internal static class AssemblyExtensions
{
    public static string GetLocalCodeBase(this Assembly assembly)
    {
#if NETFRAMEWORK
        return GetLocalCodeBase(assembly.CodeBase, Path.DirectorySeparatorChar);
#else
        return GetLocalCodeBase(assembly.Location, Path.DirectorySeparatorChar);
#endif
    }

    private static string GetLocalCodeBase(string codeBase, char directorySeparator)
    {
        if (directorySeparator is not ('/' or '\\'))
            throw new ArgumentException(
                $"Unknown directory separator '{directorySeparator}'; must be one of '/' or '\\'.",
                nameof(directorySeparator));

        var uri = new Uri(codeBase);

        if (directorySeparator == '/' && uri.IsUnc)
            throw new ArgumentException(
                $"UNC-style codebase '{codeBase}' is not supported on POSIX-style file systems.", nameof(codeBase));

        return uri.LocalPath;
    }
}
