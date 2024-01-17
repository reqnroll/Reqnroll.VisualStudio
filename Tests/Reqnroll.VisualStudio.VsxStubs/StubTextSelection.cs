using System;
using System.Collections.ObjectModel;

namespace Reqnroll.VisualStudio.VsxStubs;

public class StubTextSelection : ITextSelection
{
    private readonly StubWpfTextView _wpfTextView;
    private bool _activationTracksFocus;
    private VirtualSnapshotPoint _activePoint;
    private VirtualSnapshotPoint _anchorPoint;
    private double _leftX;
    private double _rightX;
    private TextSelectionMode _selectionMode;

    public StubTextSelection(StubWpfTextView textView)
    {
        _wpfTextView = textView;

        _activePoint = _anchorPoint = new VirtualSnapshotPoint(_wpfTextView.TextSnapshot, 0);
        _selectionMode = TextSelectionMode.Stream;
    }

    public void Select(SnapshotSpan selectionSpan, bool isReversed)
    {
        VirtualSnapshotPoint virtualSnapshotPoint1 = new VirtualSnapshotPoint(selectionSpan.Start);
        VirtualSnapshotPoint virtualSnapshotPoint2 = new VirtualSnapshotPoint(selectionSpan.End);
        if (isReversed)
            Select(virtualSnapshotPoint2, virtualSnapshotPoint1);
        else
            Select(virtualSnapshotPoint1, virtualSnapshotPoint2);
    }

    public void Select(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
    {
        if (anchorPoint.Position.Snapshot != _wpfTextView.TextSnapshot)
            throw new ArgumentException(nameof(anchorPoint));
        if (activePoint.Position.Snapshot != _wpfTextView.TextSnapshot)
            throw new ArgumentException(nameof(activePoint));
        if (anchorPoint == activePoint)
        {
            Clear(false);
        }
        else
        {
            VirtualSnapshotPoint anchorPoint1 = NormalizePoint(anchorPoint);
            VirtualSnapshotPoint activePoint1 = NormalizePoint(activePoint);
            if (anchorPoint1 == activePoint1)
                Clear(false);
            else
                InnerSelect(anchorPoint1, activePoint1);
        }
    }

    public void Clear()
    {
        Clear(true);
    }

    public VirtualSnapshotSpan? GetSelectionOnTextViewLine(ITextViewLine line) => throw new NotImplementedException();

    public ITextView TextView => throw new NotImplementedException();

    public NormalizedSnapshotSpanCollection SelectedSpans => throw new NotImplementedException();

    public ReadOnlyCollection<VirtualSnapshotSpan> VirtualSelectedSpans => throw new NotImplementedException();

    public VirtualSnapshotSpan StreamSelectionSpan => throw new NotImplementedException();

    public TextSelectionMode Mode
    {
        get => _selectionMode;
        set
        {
            if (_selectionMode == value)
                return;
            _selectionMode = value;
            if (IsEmpty)
                return;
            Select(AnchorPoint, ActivePoint);
        }
    }

    public bool IsReversed => _activePoint < _anchorPoint;

    public bool IsEmpty => _activePoint == _anchorPoint;

    public bool IsActive { set; get; }

    public bool ActivationTracksFocus
    {
        set
        {
            if (_activationTracksFocus == value)
                return;
            _activationTracksFocus = value;
            if (!_activationTracksFocus)
                return;
            IsActive = _wpfTextView.HasAggregateFocus;
        }
        get => _activationTracksFocus;
    }

    public VirtualSnapshotPoint ActivePoint
    {
        get
        {
            if (!IsEmpty)
                return _activePoint;
            return _wpfTextView.Caret.Position.VirtualBufferPosition;
        }
    }

    public VirtualSnapshotPoint AnchorPoint
    {
        get
        {
            if (!IsEmpty)
                return _anchorPoint;
            return _wpfTextView.Caret.Position.VirtualBufferPosition;
        }
    }

    public VirtualSnapshotPoint Start
    {
        get
        {
            if (!IsReversed)
                return AnchorPoint;
            return ActivePoint;
        }
    }

    public VirtualSnapshotPoint End
    {
        get
        {
            if (!IsReversed)
                return ActivePoint;
            return AnchorPoint;
        }
    }

    public event EventHandler? SelectionChanged;

    private VirtualSnapshotPoint NormalizePoint(VirtualSnapshotPoint point) => point;

    //TODO: more accurate stub?
    //ITextViewLine containingBufferPosition = (ITextViewLine)this._wpfTextView.GetTextViewLineContainingBufferPosition(point.Position);
    //if ((int)point.Position >= (int)containingBufferPosition.End)
    //    return new VirtualSnapshotPoint(containingBufferPosition.End, point.VirtualSpaces);
    //return new VirtualSnapshotPoint(containingBufferPosition.GetTextElementSpan(point.Position).Start);
    private void InnerSelect(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
    {
        bool isEmpty = IsEmpty;
        ActivationTracksFocus = true;
        _anchorPoint = anchorPoint;
        _activePoint = activePoint;
        VirtualSnapshotPoint bufferPosition1 = _anchorPoint;
        VirtualSnapshotPoint bufferPosition2 = _activePoint;
        if (_anchorPoint > _activePoint)
        {
            bufferPosition1 = _activePoint;
            bufferPosition2 = _anchorPoint;
        }

        if (Mode == TextSelectionMode.Box)
        {
            IWpfTextViewLine containingBufferPosition1 =
                _wpfTextView.GetTextViewLineContainingBufferPosition(bufferPosition1.Position);
            IWpfTextViewLine containingBufferPosition2 =
                _wpfTextView.GetTextViewLineContainingBufferPosition(bufferPosition2.Position);
            TextBounds extendedCharacterBounds = containingBufferPosition1.GetExtendedCharacterBounds(bufferPosition1);
            _leftX = extendedCharacterBounds.Leading;
            extendedCharacterBounds = containingBufferPosition2.GetExtendedCharacterBounds(bufferPosition2);
            _rightX = extendedCharacterBounds.Leading;
            if (_rightX < _leftX)
            {
                double leftX = _leftX;
                _leftX = _rightX;
                _rightX = leftX;
            }
        }
        //TODO: this.RaiseChangedEvent(isEmpty, this.IsEmpty, true);
    }

    private void Clear(bool resetMode)
    {
        bool isEmpty = IsEmpty;
        _anchorPoint = _activePoint;
        ActivationTracksFocus = true;
        if (resetMode)
            Mode = TextSelectionMode.Stream;
        //TODO this.RaiseChangedEvent(isEmpty, true, false);
    }
}
