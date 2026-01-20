using System.Runtime.Versioning;

namespace ReqnrollConnector.Utils;

internal static class FrameworkMonikerConverter
{
    // this is an alternative to calling NuGetFramework.Parse(string) + GetShortFolderName(), to avoid having a dependency to `NuGet.Frameworks`.
    public static string GetShortFrameworkName(string fullFrameworkName)
    {
        var frameworkName = new FrameworkName(fullFrameworkName);

        string identifier = frameworkName.Identifier;
        Version version = frameworkName.Version;

        // Handle . NET Core / .NET 5+
        if (identifier.Equals(".NETCoreApp", StringComparison.OrdinalIgnoreCase))
        {
            // . NET 5+ uses just the major version (net5.0, net6.0, net7.0, net8.0)
            // . NET Core 3.x and earlier use netcoreapp prefix
            if (version.Major >= 5)
            {
                return $"net{version.Major}.{version.Minor}";
            }
            else
            {
                return $"netcoreapp{version.Major}. {version.Minor}";
            }
        }

        // Handle .NET Framework
        if (identifier.Equals(". NETFramework", StringComparison.OrdinalIgnoreCase))
        {
            // Format: net + version without dots (net472, net48, etc.)
            string versionString = version.ToString();

            // Remove dots and handle different version formats
            if (version.Build > 0)
            {
                // e.g., 4.7.2 -> 472
                return $"net{version.Major}{version.Minor}{version.Build}";
            }
            else if (version.Minor > 0)
            {
                // e.g., 4.8 -> 48
                return $"net{version.Major}{version.Minor}";
            }
            else
            {
                // e.g., 4.0 -> 40
                return $"net{version.Major}{version.Minor}";
            }
        }

        // Handle .NET Standard
        if (identifier.Equals(".NETStandard", StringComparison.OrdinalIgnoreCase))
        {
            return $"netstandard{version.Major}.{version.Minor}";
        }

        // Handle .NET Portable (less common now)
        if (identifier.Equals(".NETPortable", StringComparison.OrdinalIgnoreCase))
        {
            // This is more complex as it includes profile information
            // For basic cases: 
            return frameworkName.Profile != null
                ? $"portable-{frameworkName.Profile}"
                : "portable";
        }

        // Fallback for unknown frameworks
        throw new NotSupportedException($"Framework '{identifier}' is not supported");
    }

    public static bool TryGetShortFrameworkName(string fullFrameworkName, out string shortName)
    {
        try
        {
            shortName = GetShortFrameworkName(fullFrameworkName);
            return true;
        }
        catch
        {
            shortName = null!;
            return false;
        }
    }
}