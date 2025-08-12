namespace Reqnroll.VisualStudio.Wizards.Infrastructure;

// Root object
public record NewProjectMetaRecord
{
    public List<FrameworkInfo> TestFrameworks { get; init; }
    public List<DotNetFrameworkInfo> DotNetFrameworks { get; init; }
    public List<FrameworkInfo>? ValidationFrameworks { get; init; }
}

// Describes a framework. Used to describe both Testing Frameworks(such as Nunit) and accessory frameworks (eg, Validation frameworks like FluentAssertions)
public record FrameworkInfo
{
    public string Tag { get; init; }
    public string Label { get; init; }
    public string Description { get; init; }
    public string Url { get; init; }
    public List<NugetPackageDescriptor> Dependencies { get; init; }
}

// Framework information
public record DotNetFrameworkInfo
{
    public string Tag { get; init; }
    public string Label { get; init; }
    public bool Default { get; init; }
}

public record NugetPackageDescriptor(string name, string version);
