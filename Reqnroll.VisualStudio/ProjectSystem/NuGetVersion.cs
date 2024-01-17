#nullable disable


namespace Reqnroll.VisualStudio.ProjectSystem;

public record NuGetVersion
{
    public NuGetVersion(string versionSpecifier, string requestedRange)
    {
        if (versionSpecifier == null) throw new ArgumentNullException(nameof(versionSpecifier));
        RequestedRange = requestedRange;

        var versionParts = versionSpecifier.Split(new[] {'-'}, 2);
        if (Version.TryParse(versionParts[0], out var version))
            Version = version;
        else
            Version = new Version();
        if (versionParts.Length > 1)
            PreReleaseSuffix = versionParts[1];
    }

    public Version Version { get; }
    public string PreReleaseSuffix { get; }
    public bool IsPrerelease => PreReleaseSuffix != null;

    /// <summary>The project's requested package range for the package.</summary>
    /// <remarks>
    ///     If the project uses packages.config, this will be same as the installed package version.
    ///     If the project uses PackageReference, this is the version string in the project file, which may not match the
    ///     resolved package version, and may not be single version string.
    ///     If the project uses PackageReference, and the package is a transitive dependency, the value will be null.
    /// </remarks>
    public string RequestedRange { get; }

    public bool IsFloating => RequestedRange != null && Regex.IsMatch(RequestedRange, @"\*");

    public override string ToString()
    {
        var str = IsPrerelease ? $"{Version}-{PreReleaseSuffix}" : Version.ToString();
        if (IsFloating) str += $"({RequestedRange})";
        return str;
    }

    public string ToShortVersionString() => $"{Version.Major}{Version.Minor:00}{Version.Build}";
}
