namespace Reqnroll.VisualStudio.VsxStubs;

public class StubTextCaret : ITextCaret
{
    private readonly ITextBuffer _textBuffer;
    private readonly StubWpfTextView _wpfTextView;
    private PositionAffinity _caretAffinity;
    private VirtualSnapshotPoint _insertionPoint;

    public StubTextCaret(StubWpfTextView wpfTextView)
    {
        _wpfTextView = wpfTextView;
        _textBuffer = wpfTextView.TextBuffer;
        _insertionPoint = new VirtualSnapshotPoint(new SnapshotPoint(_textBuffer.CurrentSnapshot, 0));
        _caretAffinity = PositionAffinity.Successor;
    }

    public void EnsureVisible()
    {
        throw new NotImplementedException();
    }

    public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate) => throw new NotImplementedException();

    public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition) =>
        throw new NotImplementedException();

    public CaretPosition MoveTo(ITextViewLine textLine) => throw new NotImplementedException();

    public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition)
    {
        InternalMoveTo(bufferPosition, PositionAffinity.Successor, true, true, true);
        return Position;
    }

    public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity)
    {
        InternalMoveTo(bufferPosition, caretAffinity, true, true, true);
        return Position;
    }

    public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity,
        bool captureHorizontalPosition)
    {
        InternalMoveTo(bufferPosition, caretAffinity, captureHorizontalPosition, true, true);
        return Position;
    }

    public CaretPosition MoveTo(SnapshotPoint bufferPosition)
    {
        InternalMoveTo(new VirtualSnapshotPoint(bufferPosition), PositionAffinity.Successor, true, true, true);
        return Position;
    }

    public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity)
    {
        InternalMoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity, true, true, true);
        return Position;
    }

    public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity,
        bool captureHorizontalPosition)
    {
        InternalMoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity, captureHorizontalPosition, true, true);
        return Position;
    }


    public CaretPosition MoveToPreferredCoordinates() => throw new NotImplementedException();

    public CaretPosition MoveToNextCaretPosition() => throw new NotImplementedException();

    public CaretPosition MoveToPreviousCaretPosition() => throw new NotImplementedException();

    public ITextViewLine ContainingTextViewLine => throw new NotImplementedException();

    public double Left => throw new NotImplementedException();

    public double Width => throw new NotImplementedException();

    public double Right => throw new NotImplementedException();

    public double Top => throw new NotImplementedException();

    public double Height => throw new NotImplementedException();

    public double Bottom => throw new NotImplementedException();

    public CaretPosition Position => new(_insertionPoint,
        VsxStubObjects.BufferGraphFactoryService.CreateBufferGraph(_textBuffer)
            .CreateMappingPoint(_insertionPoint.Position, PointTrackingMode.Positive), _caretAffinity);

    public bool OverwriteMode => throw new NotImplementedException();

    public bool InVirtualSpace => throw new NotImplementedException();

    public bool IsHidden
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public event EventHandler<CaretPositionChangedEventArgs>? PositionChanged;

    private void InternalMoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity,
        bool captureHorizontalPosition, bool captureVerticalPosition, bool raiseEvent)
    {
        if (bufferPosition.Position.Snapshot != _wpfTextView.TextSnapshot)
            throw new ArgumentException(nameof(bufferPosition));
        _insertionPoint = bufferPosition;
        _caretAffinity = caretAffinity;

        //ITextViewLine containingTextViewLine = this.GetContainingTextViewLine(bufferPosition.Position, caretAffinity);
        //VirtualSnapshotPoint bufferPosition1 = CaretElement.NormalizePosition(bufferPosition, containingTextViewLine);
        //if (bufferPosition1 != bufferPosition)
        //    raiseEvent = true;
        //this.InternalMoveCaret(bufferPosition1, caretAffinity, containingTextViewLine, captureHorizontalPosition, captureVerticalPosition, raiseEvent);
    }
}
