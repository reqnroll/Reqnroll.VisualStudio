#nullable disable
namespace Reqnroll.VisualStudio.VsxStubs;

public class StubWpfTextView : IWpfTextView
{
    private readonly Dictionary<string, IAdornmentLayer> _adornmentLayers = new();
    private readonly StubTextCaret _caret;

    public StubWpfTextView(ITextBuffer textBuffer)
    {
        TextBuffer = textBuffer;
        _caret = new StubTextCaret(this);
        Selection = new StubTextSelection(this);
        ViewScroller = new StubViewScroller();
    }

    public StubEditorOptions StubEditorOptions { get; } = new();

    public PropertyCollection Properties { get; } = new();

    public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance,
        ViewRelativePosition relativeTo)
    {
        throw new NotImplementedException();
    }

    public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance,
        ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride)
    {
        throw new NotImplementedException();
    }

    public SnapshotSpan GetTextElementSpan(SnapshotPoint point) => throw new NotImplementedException();

    public void Close()
    {
        throw new NotImplementedException();
    }

    public void QueueSpaceReservationStackRefresh()
    {
        throw new NotImplementedException();
    }

    public IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) =>
        throw new NotImplementedException();

    public FrameworkElement VisualElement => throw new NotImplementedException();

    public Brush Background
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public IAdornmentLayer GetAdornmentLayer(string name)
    {
        if (!_adornmentLayers.TryGetValue(name, out var adornmentLayer))
        {
            //adornmentLayer = VsxStubObjects.CreateObject<IAdornmentLayer>(
            //    "Microsoft.VisualStudio.Text.Editor.Implementation.AdornmentLayer, Microsoft.VisualStudio.Platform.VSEditor",
            //    this, name, false);
            adornmentLayer = new StubAdornmentLayer();
            _adornmentLayers[name] = adornmentLayer;
        }

        return adornmentLayer;
    }

    public ISpaceReservationManager GetSpaceReservationManager(string name) => throw new NotImplementedException();

    ITextViewLine ITextView.GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition) =>
        GetTextViewLineContainingBufferPosition(bufferPosition);

    public bool InLayout => true; //in a process of layout

    public IViewScroller ViewScroller { get; }

    public IWpfTextViewLineCollection TextViewLines => throw new NotImplementedException();

    public IFormattedLineSource FormattedLineSource => throw new NotImplementedException();

    public ILineTransformSource LineTransformSource => throw new NotImplementedException();

    public double ZoomLevel
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public event EventHandler<BackgroundBrushChangedEventArgs> BackgroundBrushChanged;
    public event EventHandler<ZoomLevelChangedEventArgs> ZoomLevelChanged;

    ITextViewLineCollection ITextView.TextViewLines => TextViewLines;

    public ITextCaret Caret => _caret;

    public ITextSelection Selection { get; }

    public ITrackingSpan ProvisionalTextHighlight
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public ITextViewRoleSet Roles => throw new NotImplementedException();

    public ITextBuffer TextBuffer { get; }

    public IBufferGraph BufferGraph => throw new NotImplementedException();

    public ITextSnapshot TextSnapshot => TextBuffer.CurrentSnapshot;

    public ITextSnapshot VisualSnapshot => throw new NotImplementedException();

    public ITextViewModel TextViewModel => throw new NotImplementedException();

    public ITextDataModel TextDataModel => throw new NotImplementedException();

    public double MaxTextRightCoordinate => throw new NotImplementedException();

    public double ViewportLeft
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public double ViewportTop => throw new NotImplementedException();

    public double ViewportRight => throw new NotImplementedException();

    public double ViewportBottom => throw new NotImplementedException();

    public double ViewportWidth => throw new NotImplementedException();

    public double ViewportHeight => throw new NotImplementedException();

    public double LineHeight => throw new NotImplementedException();

    public bool IsClosed { get; } = false; // Simulate view to be open

    public IEditorOptions Options => StubEditorOptions;

    public bool IsMouseOverViewOrAdornments => throw new NotImplementedException();

    public bool HasAggregateFocus { get; } = true; // Simulate view to be in focus

    public event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged;
    public event EventHandler ViewportLeftChanged;
    public event EventHandler ViewportHeightChanged;
    public event EventHandler ViewportWidthChanged;
    public event EventHandler<MouseHoverEventArgs> MouseHover;
    public event EventHandler Closed;
    public event EventHandler LostAggregateFocus;
    public event EventHandler GotAggregateFocus;

    public static StubWpfTextView CreateTextView(TestText inputText, Func<TestText, ITextBuffer> textBufferFactory)
    {
        var textBuffer = textBufferFactory(inputText);

        var textView = new StubWpfTextView(textBuffer);
        inputText.SetSelection(textView);
        inputText.SetCaret(textView);

        return textView;
    }

    public void SimulateTypeText(DeveroomEditorTypeCharCommandBase command, string text, ITaggerProvider taggerProvider)
    {
        foreach (var ch in text) SimulateType(command, ch, taggerProvider);
    }

    public void SimulateType(DeveroomEditorTypeCharCommandBase command, char c, ITaggerProvider taggerProvider)
    {
        var caretPosition = Caret.Position.BufferPosition.Position;
        using (var textEdit = TextBuffer.CreateEdit())
        {
            textEdit.Insert(Caret.Position.BufferPosition.Position, c.ToString());
            textEdit.Apply();
        }

        Caret.MoveTo(new SnapshotPoint(TextSnapshot, caretPosition + 1));
        ForceReparse(taggerProvider); //this is needed because currently partial table formatting is not supported

        command.PostExec(this, c);
    }

    public void ForceReparse(ITaggerProvider taggerProvider)
    {
        var tagger = taggerProvider.CreateTagger<DeveroomTag>(TextBuffer);
        var span = new SnapshotSpan(TextSnapshot, 0, TextSnapshot.Length);
        tagger.GetUpToDateDeveroomTagsForSpan(span);
    }
}
