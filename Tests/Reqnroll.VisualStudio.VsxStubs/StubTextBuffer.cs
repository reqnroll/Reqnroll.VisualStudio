namespace Reqnroll.VisualStudio.VsxStubs;

public class StubTextBuffer : Mock<ITextBuffer2>, ITextBuffer2
{
    public StubTextBuffer(IProjectScope projectScope) : base(MockBehavior.Strict)
    {
        Properties = new PropertyCollection();
        Properties.AddProperty(typeof(IProjectScope), projectScope);
        CurrentStubSnapshot = StubTextSnapshot.FromTextBuffer(this);

        var contentType = new Mock<IContentType>(MockBehavior.Strict);
        contentType.Setup(t => t.IsOfType(VsContentTypes.FeatureFile)).Returns(true);
        StubContentType = new StubContentType(Array.Empty<IContentType>(), VsContentTypes.FeatureFile,
            VsContentTypes.FeatureFile);

        SetupAdd(tb => tb.ChangedOnBackground += It.IsAny<EventHandler<TextContentChangedEventArgs>>());
        SetupRemove(tb => tb.ChangedOnBackground -= It.IsAny<EventHandler<TextContentChangedEventArgs>>());
    }

    public StubContentType StubContentType { get; set; }

    public StubTextSnapshot CurrentStubSnapshot { get; private set; }

    public PropertyCollection Properties { get; }

    public ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag) =>
        throw new NotImplementedException();

    public ITextEdit CreateEdit() => throw new NotImplementedException();

    public IReadOnlyRegionEdit CreateReadOnlyRegionEdit() => throw new NotImplementedException();

    public void TakeThreadOwnership()
    {
        throw new NotImplementedException();
    }

    public bool CheckEditAccess() => throw new NotImplementedException();

    public void ChangeContentType(IContentType newContentType, object editTag)
    {
        throw new NotImplementedException();
    }

    public ITextSnapshot Insert(int position, string text) => throw new NotImplementedException();

    public ITextSnapshot Delete(Span deleteSpan) => throw new NotImplementedException();

    public ITextSnapshot Replace(Span replaceSpan, string replaceWith) => throw new NotImplementedException();

    public bool IsReadOnly(int position) => throw new NotImplementedException();

    public bool IsReadOnly(int position, bool isEdit) => throw new NotImplementedException();

    public bool IsReadOnly(Span span) => throw new NotImplementedException();

    public bool IsReadOnly(Span span, bool isEdit) => throw new NotImplementedException();

    public NormalizedSpanCollection GetReadOnlyExtents(Span span) => throw new NotImplementedException();

    public IContentType ContentType => StubContentType;
    public ITextSnapshot CurrentSnapshot => CurrentStubSnapshot;
    public bool EditInProgress { get; }
    public event EventHandler<SnapshotSpanEventArgs>? ReadOnlyRegionsChanged;
    public event EventHandler<TextContentChangedEventArgs>? Changed;
    public event EventHandler<TextContentChangedEventArgs>? ChangedLowPriority;
    public event EventHandler<TextContentChangedEventArgs>? ChangedHighPriority;
    public event EventHandler<TextContentChangingEventArgs>? Changing;
    public event EventHandler? PostChanged;
    public event EventHandler<ContentTypeChangedEventArgs>? ContentTypeChanged;

    public event EventHandler<TextContentChangedEventArgs>? ChangedOnBackground
    {
        add
        {
            Object.ChangedOnBackground += value;
            _changedOnBackground += value;
        }
        remove
        {
            Object.ChangedOnBackground -= value;
            _changedOnBackground -= value;
        }
    }

    private event EventHandler<TextContentChangedEventArgs>? _changedOnBackground;

    public void InvokeChanged()
    {
        Changed?.Invoke(this,
            new TextContentChangedEventArgs(CurrentSnapshot, CurrentSnapshot, EditOptions.None, string.Empty));
    }

    public void InvokeChangedOnBackground()
    {
        var beforeSnapshot = CurrentStubSnapshot;
        var afterSnapshot = CurrentStubSnapshot = CurrentStubSnapshot.CreateNext();

        //VS invokes this event multiple times for some reason
        _changedOnBackground?.Invoke(this,
            new TextContentChangedEventArgs(beforeSnapshot, afterSnapshot, EditOptions.None, string.Empty));
        _changedOnBackground?.Invoke(this,
            new TextContentChangedEventArgs(beforeSnapshot, afterSnapshot, EditOptions.None, string.Empty));
        _changedOnBackground?.Invoke(this,
            new TextContentChangedEventArgs(beforeSnapshot, afterSnapshot, EditOptions.None, string.Empty));
        _changedOnBackground?.Invoke(this,
            new TextContentChangedEventArgs(beforeSnapshot, afterSnapshot, EditOptions.None, string.Empty));
    }

    public void ModifyContent(string content)
    {
        CurrentStubSnapshot = CurrentStubSnapshot.WithText(content);
    }
}
