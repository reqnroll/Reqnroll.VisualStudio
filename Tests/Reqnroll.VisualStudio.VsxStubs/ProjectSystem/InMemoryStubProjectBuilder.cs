namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class InMemoryStubProjectBuilder : IDisposable
{
    private readonly AsyncManualResetEvent _bindingRegistryChanged;
    private readonly DebuggableCancellationTokenSource _cts;
    private readonly InMemoryStubProjectScope _project;
    private readonly AsyncManualResetEvent _tagChanged;
    private readonly ITagger<DeveroomTag> _tagger;
    private readonly IProjectBindingRegistryCache bindingRegistryCache;

    public InMemoryStubProjectBuilder(InMemoryStubProjectScope project)
    {
        _project = project;
        bindingRegistryCache = project.Properties.GetProperty<IDiscoveryService>(typeof(IDiscoveryService))
            .BindingRegistryCache;

        var taggerProvider = new DeveroomTaggerProvider(project.IdeScope);
        _tagger = taggerProvider.CreateTagger<DeveroomTag>(VisibleTextBuffer);

        _bindingRegistryChanged = new AsyncManualResetEvent();
        _tagChanged = new AsyncManualResetEvent();
        _cts = new DebuggableCancellationTokenSource(TimeSpan.FromSeconds(10));
    }

    private StubIdeScope StubIdeScope => _project.StubIdeScope;
    private ITextBuffer VisibleTextBuffer => StubIdeScope.CurrentTextView.TextBuffer;
    private StubProjectSettingsProvider ProjectSettingsProvider => _project.StubProjectSettingsProvider;


    public void Dispose()
    {
        _tagger.TagsChanged -= TaggerOnTagsChanged;
        bindingRegistryCache.Changed -= OnBindingRegistryCacheOnChanged;
        _cts.Dispose();
    }


    public static FileInfo CreateOutputAssembly(IProjectScope project)
    {
        FileInfo fi = new FileInfo(project.OutputAssemblyPath);
        project.IdeScope.FileSystem.Directory.CreateDirectory(fi.DirectoryName);
        project.IdeScope.FileSystem.File.WriteAllText(fi.FullName, string.Empty);
        return fi;
    }

    public InMemoryStubProjectBuilder TriggerBuild()
    {
        bindingRegistryCache.Changed += OnBindingRegistryCacheOnChanged;

        _tagger.TagsChanged += TaggerOnTagsChanged;

        var fi = CreateOutputAssembly(_project);

        var file = new MockFile(_project.IdeScope.FileSystem as MockFileSystem);
        file.SetLastWriteTimeUtc(fi.FullName, DateTime.UtcNow);

        StubIdeScope.TriggerProjectsBuilt();

        return this;
    }

    public static async Task BuildAndWaitBackGroundTasks(InMemoryStubProjectScope project)
    {
        using var builder = new InMemoryStubProjectBuilder(project);

        await builder
            .TriggerBuild()
            .WaitBackgroundTasksToFinish();
    }

    private void OnBindingRegistryCacheOnChanged(object o, EventArgs eventArgs) => _bindingRegistryChanged.Set();

    private void TaggerOnTagsChanged(object sender, SnapshotSpanEventArgs e)
    {
        _tagChanged.Set();
    }

    public async Task WaitBackgroundTasksToFinish()
    {
        if (BindingRegistryIsExpected()) await _bindingRegistryChanged.WaitAsync(_cts.Token);
        if (ConfigurationChanged() && FeatureFileIsDisplayed()) await _tagChanged.WaitAsync(_cts.Token);
    }

    private bool BindingRegistryIsExpected() =>
        ProjectSettingsProvider.Kind != DeveroomProjectKind.FeatureFileContainerProject;

    private bool ConfigurationChanged() => true;

    private bool FeatureFileIsDisplayed() => VisibleTextBuffer.ContentType.TypeName == VsContentTypes.FeatureFile;
}
