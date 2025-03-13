namespace Reqnroll.VisualStudio.VsxStubs;

public record StubTextSnapshotLine(
    SnapshotPoint End,
    SnapshotPoint EndIncludingLineBreak,
    SnapshotSpan Extent,
    SnapshotSpan ExtentIncludingLineBreak,
    int Length,
    int LengthIncludingLineBreak,
    int LineBreakLength,
    int LineNumber,
    ITextSnapshot Snapshot,
    SnapshotPoint Start) : ITextSnapshotLine
{
    public string GetText() => throw new NotImplementedException();

    public string GetTextIncludingLineBreak() => throw new NotImplementedException();

    public string GetLineBreakText() => throw new NotImplementedException();
}

public record StubTextSnapshot(
    IContentType ContentType,
    int Length,
    int LineCount,
    IEnumerable<ITextSnapshotLine> Lines,
    ITextBuffer TextBuffer,
    ITextVersion Version,
    ITextImage TextImage,
    TestText TestText) : ITextSnapshot2
{
    public string GetText(Span span) => TestText.ToString();

    public string GetText(int startIndex, int length) => throw new NotImplementedException();

    public string GetText() => TestText.ToString();

    public char[] ToCharArray(int startIndex, int length) => throw new NotImplementedException();

    public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    {
        throw new NotImplementedException();
    }

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

    public ITextSnapshotLine GetLineFromLineNumber(int lineNumber) => throw new NotImplementedException();

    public ITextSnapshotLine GetLineFromPosition(int position) => throw new NotImplementedException();

    public int GetLineNumberFromPosition(int position) => throw new NotImplementedException();

    public void Write(TextWriter writer, Span span)
    {
        throw new NotImplementedException();
    }

    public void Write(TextWriter writer)
    {
        throw new NotImplementedException();
    }

    public char this[int position] => throw new NotImplementedException();

    public void SaveToFile(string filePath, bool replaceFile, Encoding encoding)
    {
        throw new NotImplementedException();
    }

    public static StubTextSnapshot FromTextBuffer(ITextBuffer textBuffer) =>
        new(
            Substitute.For<IContentType>(),
            0,
            0,
            Array.Empty<ITextSnapshotLine>(),
            textBuffer,
            StubTextVersion2.Default with {TextBuffer = textBuffer},
            Substitute.For<ITextImage>(),
            new TestText());

    public StubTextSnapshot WithText(string testText)
    {
        var result = this with
        {
            Length = testText.Length
        };
        var snapshotLines = result.ToSnapshotLines(testText);
        result = result with
        {
            Lines = snapshotLines,
            LineCount = snapshotLines.Count,
            Length = testText.Length,
            Version = (Version as StubTextVersion2)!.CreateNext()
        };

        return result;
    }

    private List<StubTextSnapshotLine> ToSnapshotLines(string content)
    {
        var newLine = Environment.NewLine;
        var snapShotLines = new List<StubTextSnapshotLine>();
        var processedLength = 0;

        var result = content.Split(new[] {newLine}, StringSplitOptions.None);
        for (int lineIndex = 0; lineIndex < result.Length; lineIndex++)
        {
            var line = result[lineIndex];

            var start = new SnapshotPoint(this, processedLength);
            var end = new SnapshotPoint(this, processedLength + line.Length);
            var endIncludingLineBreak =
                new SnapshotPoint(this, Math.Min(processedLength + line.Length + newLine.Length, Length));
            var extent = new SnapshotSpan(start, end);
            var extentIncludingLineBreak = new SnapshotSpan(start, endIncludingLineBreak);

            var sl = new StubTextSnapshotLine(
                end,
                endIncludingLineBreak,
                extent,
                extentIncludingLineBreak,
                line.Length,
                line.Length + newLine.Length,
                newLine.Length,
                lineIndex,
                this,
                start);

            snapShotLines.Add(sl);
            processedLength += line.Length + newLine.Length;
        }

        return snapShotLines;
    }


    public StubTextSnapshot CreateNext() => this with {Version = (Version as StubTextVersion2)!.CreateNext()};
}
