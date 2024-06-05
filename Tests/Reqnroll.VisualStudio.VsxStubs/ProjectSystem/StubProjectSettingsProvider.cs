namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class StubProjectSettingsProvider : Mock<IProjectSettingsProvider>, IProjectSettingsProvider
{
    public StubProjectSettingsProvider(InMemoryStubProjectScope inMemoryStubProjectScope) : base(MockBehavior.Strict)
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

        Setup(p => p.GetProjectSettings()).Returns(() => ProjectSettings);
        Setup(p => p.CheckProjectSettings()).Returns(() => ProjectSettings);
    }

    private ProjectSettings ProjectSettings { get; set; }

    public DeveroomProjectKind Kind
    {
        get => ProjectSettings.Kind;
        set => ProjectSettings = ProjectSettings with {Kind = value};
    }

    public event EventHandler<EventArgs>? WeakSettingsInitialized;
    public event EventHandler<EventArgs>? SettingsInitialized;

    public ProjectSettings GetProjectSettings() => Object.GetProjectSettings();
    public ProjectSettings CheckProjectSettings() => Object.CheckProjectSettings();

    public void InvokeWeakSettingsInitializedEvent()
    {
        WeakSettingsInitialized!.Invoke(this, EventArgs.Empty);
    }
}
