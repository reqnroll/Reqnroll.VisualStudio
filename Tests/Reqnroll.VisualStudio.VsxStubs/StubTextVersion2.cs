namespace Reqnroll.VisualStudio.VsxStubs;

public record StubTextVersion2(
        INormalizedTextChangeCollection Changes,
        int Length,
        int ReiteratedVersionNumber,
        ITextBuffer TextBuffer,
        int VersionNumber,
        ITextImageVersion ImageVersion)
    : ITextVersion2
{
    public static StubTextVersion2 Default = new(
        Mock.Of<INormalizedTextChangeCollection>(m => m.Count == 0, MockBehavior.Strict),
        0,
        0, Mock.Of<ITextBuffer>(MockBehavior.Strict),
        0, Mock.Of<ITextImageVersion>(MockBehavior.Strict));

    [CanBeNull] public ITextVersion Next { get; private set; } = null!;

    public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode) =>
        throw new NotImplementedException();

    public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode,
        TrackingFidelityMode trackingFidelity) => throw new NotImplementedException();

    public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode) =>
        throw new NotImplementedException();

    public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode,
        TrackingFidelityMode trackingFidelity) => throw new NotImplementedException();

    public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode) =>
        throw new NotImplementedException();

    public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode,
        TrackingFidelityMode trackingFidelity) =>
        throw new NotImplementedException();

    public ITrackingSpan CreateCustomTrackingSpan(Span span, TrackingFidelityMode trackingFidelity, object customState,
        CustomTrackToVersion behavior) =>
        throw new NotImplementedException();

    private bool CircularInvocation()
    {
        StackTrace stackTrace = new StackTrace();
        StackFrame[] stackFrames = stackTrace.GetFrames();
        var b = stackFrames?[2].GetMethod().Name == nameof(PrintMembers);
        return b;
    }

    public ITextVersion CreateNext()
    {
        Next = this with {VersionNumber = VersionNumber + 1};
        return Next;
    }
}
