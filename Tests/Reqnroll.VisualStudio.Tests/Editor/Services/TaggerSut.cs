using NSubstitute;

namespace Reqnroll.VisualStudio.Tests.Editor.Services;

public record TaggerSut
    (IProjectScope ProjectScope, StubIdeScope IdeScope, IDeveroomTagParser TagParser) : IDisposable
{
    private readonly ManualResetEvent _tagsChanged = new(false);
    private readonly List<SnapshotSpanEventArgs> _tagsChangedEvents = new();

    public IReadOnlyCollection<SnapshotSpanEventArgs> TagsChangedEvents => _tagsChangedEvents;

    public IEnumerable<LogMessage> LoggerMessages => IdeScope.StubLogger.Logs;

    public IEnumerable<LogMessage> LoggerErrorMessages =>
        LoggerMessages.Where(m => m.Level == TraceLevel.Error || m.Message.Contains("Exception"));

    public StubTextBuffer StubTextBuffer => (IdeScope.CurrentTextView.TextBuffer as StubTextBuffer)!;

    public SnapshotSpan CurrentSnapshotSpan => new(IdeScope.CurrentTextView.Caret.Position.BufferPosition, 0);
    public NormalizedSnapshotSpanCollection CurrentSnapshotSpanCollection => new(CurrentSnapshotSpan);

    public void Dispose()
    {
        AssertNoErrorLogged();
        IdeScope.Dispose();
    }

    public static TaggerSut Arrange(ITestOutputHelper testOutputHelper)
    {
        var projectScope = new InMemoryStubProjectScope(testOutputHelper);
        var tagParser = Substitute.For<IDeveroomTagParser>();

        var deveroomTags = ImmutableArray<DeveroomTag>.Empty;
        tagParser
            .Parse(Arg.Any<ITextSnapshot>())
            .Returns(deveroomTags)
            .AndDoes(callInfo => { projectScope.IdeScope.Logger.Trace($"Parsing {callInfo.Arg<ITextSnapshot>()}"); });

        projectScope.StubIdeScope.TextViewFactory =
            (inputText, filePath) => new StubWpfTextView(new StubTextBuffer(projectScope));

        var sut = new TaggerSut(projectScope, projectScope.StubIdeScope, tagParser);

        VsxStubObjects.Initialize();

        return sut;
    }

    public ITagger<DeveroomTag> BuildInitializedFeatureFileTagger()
    {
        var tagger = BuildFeatureFileTagger();
        tagger.GetUpToDateDeveroomTagsForSpan(CurrentSnapshotSpan);
        return tagger;
    }

    public ITagger<DeveroomTag> BuildFeatureFileTagger()
    {
        ProjectScope.Properties.AddProperty(typeof(IDeveroomTagParser), TagParser);
        var taggerProvider = new DeveroomTaggerProvider(IdeScope, new SpecFlowExtensionDetection.SpecFlowExtensionDetectionService(IdeScope));

        var tagger = BuildTagger<FeatureFileTagger>(taggerProvider);
        tagger.TagsChanged += DeveroomTagger_TagsChanged;
        SimulateTagsChangedIfNecessary(tagger);

        return tagger;
    }

    private T BuildTagger<T>(DeveroomTaggerProvider taggerProvider) where T : ITagger<DeveroomTag>
    {
        IdeScope.CreateTextView(new TestText(Array.Empty<string>()),
            IdeScope.ProjectScopes.Select(p => p.ProjectFullName).DefaultIfEmpty(string.Empty).Single());

        var tagger = (T) taggerProvider.CreateTagger<DeveroomTag>(IdeScope.CurrentTextView.TextBuffer);

        return tagger;
    }

    private void SimulateTagsChangedIfNecessary(FeatureFileTagger tagger)
    {
        if (tagger.ParsedSnapshotVersionNumber ==
            IdeScope.CurrentTextView.TextBuffer.CurrentSnapshot.Version.VersionNumber)
            DeveroomTagger_TagsChanged(tagger, new SnapshotSpanEventArgs(new SnapshotSpan()));
    }

    private void DeveroomTagger_TagsChanged(object sender, SnapshotSpanEventArgs e)
    {
        _tagsChangedEvents.Add(e);
        _tagsChanged.Set();
    }

    public IReadOnlyCollection<SnapshotSpanEventArgs> WaitForTagsChangedEvent()
    {
        if (_tagsChanged.WaitOne(DebuggableCancellationTokenSource.GetDebuggerTimeout(TimeSpan.FromSeconds(5))))
        {
            _tagsChanged.Reset();
            return TagsChangedEvents;
        }

        throw new InvalidOperationException("TagsChanged event not fired in time");
    }

    private void AssertNoErrorLogged()
    {
        LoggerErrorMessages.Should().BeEmpty();
    }

    public TaggerSut WithRealDeveroomTagParser()
    {
        var deveroomConfigurationProvider = ProjectScope.GetDeveroomConfigurationProvider();
        var discoveryService = ProjectScope.GetDiscoveryService();
        var realParser = new DeveroomTagParser(IdeScope.Logger, IdeScope.MonitoringService,
            deveroomConfigurationProvider, discoveryService);
        var tagParserMock = Substitute.For<IDeveroomTagParser>();
        tagParserMock.Parse(Arg.Any<ITextSnapshot>())
            .Returns(callInfo => realParser.Parse(callInfo.Arg<ITextSnapshot>()));

        return this with {TagParser = tagParserMock};
    }

    public TaggerSut WithoutProject()
    {
        var voidProjectScope = new VoidProjectScope(IdeScope);
        var withoutProject = this with {ProjectScope = voidProjectScope};
        IdeScope.ProjectScopes.Clear();
        IdeScope.TextViewFactory =
            (inputText, filePath) => new StubWpfTextView(new StubTextBuffer(voidProjectScope));
        return withoutProject;
    }
}
