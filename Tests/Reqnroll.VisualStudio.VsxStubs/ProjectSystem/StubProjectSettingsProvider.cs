namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class StubProjectSettingsProvider : IProjectSettingsProvider
{
    public StubProjectSettingsProvider(InMemoryStubProjectScope inMemoryStubProjectScope)
    {
        ProjectSettings = new ProjectSettings(
            DeveroomProjectKind.ReqnrollTestProject,
            TargetFrameworkMoniker.CreateFromShortName("net6"),
            $"{TargetFrameworkMoniker.CreateFromShortName("net6").Value};{TargetFrameworkMoniker.CreateFromShortName("net48").Value}",
            ProjectPlatformTarget.AnyCpu,
            inMemoryStubProjectScope.OutputAssemblyPath,
            "TestProject",
            new NuGetVersion("3.9.40", "3.9.40"),
            string.Empty,
            string.Empty,
            ReqnrollProjectTraits.CucumberExpression,
            ProjectProgrammingLanguage.CSharp
        );
    }

    private ProjectSettings ProjectSettings { get; set; }

    public DeveroomProjectKind Kind
    {
        get => ProjectSettings.Kind;
        set => ProjectSettings = ProjectSettings with { Kind = value };
    }

    public event EventHandler<EventArgs>? WeakSettingsInitialized;
    public event EventHandler<EventArgs>? SettingsInitialized;

    public ProjectSettings GetProjectSettings() => ProjectSettings;
    public ProjectSettings CheckProjectSettings() => ProjectSettings;

    public void InvokeWeakSettingsInitializedEvent()
    {
        WeakSettingsInitialized?.Invoke(this, EventArgs.Empty);
    }
}
