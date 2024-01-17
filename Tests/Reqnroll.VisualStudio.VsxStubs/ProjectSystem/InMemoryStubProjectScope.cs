namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class InMemoryStubProjectScope : Mock<IProjectScope>, IProjectScope
{
    public List<NuGetPackageReference> PackageReferencesList = new();

    public InMemoryStubProjectScope(StubIdeScope stubIdeScope)
    {
        StubIdeScope = stubIdeScope;

        StubProjectSettingsProvider = new StubProjectSettingsProvider(this);
        Properties.AddProperty(typeof(IProjectSettingsProvider), StubProjectSettingsProvider);
        var configProvider = CreateConfigurationProvider();
        Properties.AddProperty(typeof(IDeveroomConfigurationProvider), configProvider);
        StubIdeScope.ProjectScopes.Add(this);
    }

    public InMemoryStubProjectScope(ITestOutputHelper testOutputHelper)
        : this(new StubIdeScope(testOutputHelper))
    {
    }

    public StubIdeScope StubIdeScope { get; }
    public StubProjectSettingsProvider StubProjectSettingsProvider { get; }
    public Dictionary<string, string> FilesAdded { get; } = new();
    public string ProjectFileName => ProjectName + ".csproj";
    public PropertyCollection Properties { get; } = new();
    public IIdeScope IdeScope => StubIdeScope;
    public IEnumerable<NuGetPackageReference> PackageReferences => PackageReferencesList;
    public string ProjectFolder { get; } = Path.GetTempPath();
    public string OutputAssemblyPath => Path.Combine(ProjectFolder, "out.dll");
    public string TargetFrameworkMoniker { get; } = ".NETFramework,Version=v4.5.2";
    public string TargetFrameworkMonikers { get; } = ".NETCoreApp,Version=v5.0;.NETFramework,Version=v4.5.2";
    public string PlatformTargetName { get; } = "Any CPU";
    public string ProjectName { get; } = "Test Project";
    public string ProjectFullName => Path.Combine(ProjectFolder, ProjectFileName);
    public string DefaultNamespace => ProjectName.Replace(" ", "");

    public void AddFile(string targetFilePath, string template)
    {
        if (!Path.IsPathRooted(targetFilePath))
            targetFilePath = Path.Combine(ProjectFolder, targetFilePath);

        StubIdeScope.FileSystem.Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));
        StubIdeScope.FileSystem.File.WriteAllText(targetFilePath, template);
        FilesAdded[targetFilePath] = template;
    }

    public int? GetFeatureFileCount()
    {
        return FilesAdded.Keys.Count(f => f.EndsWith(".feature"));
    }

    public string[] GetProjectFiles(string extension)
    {
        return FilesAdded.Keys.Where(f => FileSystemHelper.IsOfType(f, extension))
            .ToArray();
    }

    public void Dispose()
    {
    }

    private ProjectScopeDeveroomConfigurationProvider CreateConfigurationProvider()
    {
        AddFile(ProjectFullName, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        var configProvider = new ProjectScopeDeveroomConfigurationProvider(this);
        return configProvider;
    }

    public void AddReqnrollPackage()
    {
        PackageReferencesList.Add(new NuGetPackageReference("Reqnroll", new NuGetVersion("2.3.2", "2.3.2"),
            Path.Combine(ProjectFolder, "packages", "Reqnroll")));
    }

    public InMemoryStubProjectScope UpdateConfigFile(string configFileName, string configFileContent)
    {
        var configFileFullPath = Path.Combine(ProjectFolder, configFileName);
        IdeScope.FileSystem.File
            .WriteAllText(configFileFullPath, configFileContent, Encoding.UTF8);
        IdeScope.FileSystem.File.SetLastWriteTimeUtc(configFileFullPath, DateTime.UtcNow);
        return this;
    }
}
