#nullable disable

using Reqnroll.VisualStudio.ProjectSystem;

namespace Reqnroll.VisualStudio.Editor.Completions.Infrastructure;

public abstract class DeveroomCompletionSourceBase : ICompletionSource
{
    protected readonly ITextBuffer _buffer;
    private readonly IIdeScope _ideScope;
    private readonly string _name;

    protected DeveroomCompletionSourceBase(string name, ITextBuffer buffer, IIdeScope ideScope)
    {
        _name = name;
        _buffer = buffer;
        _ideScope = ideScope;
    }

    public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
    {
        var snapshotTriggerPoint = session.GetTriggerPoint(_buffer.CurrentSnapshot);
        if (snapshotTriggerPoint == null)
            return;

        var sw = Stopwatch.StartNew();
        var completionResult = CollectCompletions(snapshotTriggerPoint.Value);
        if (completionResult.Value.Count == 0)
            return;

        var applicableTo = GetApplicableTo(completionResult);
        if (applicableTo == null)
            return;

        _ideScope.Logger.Trace(sw, $"Completions collected in {sw.ElapsedMilliseconds} ms: {completionResult.Value.Count}");

        completionSets.Add(new WordContainsFilteredCompletionSet(
            _name,
            _name,
            applicableTo,
            completionResult.Value,
            null));
    }

    public void Dispose()
    {
        //nop
    }

    protected abstract KeyValuePair<SnapshotSpan, List<Completion>> CollectCompletions(SnapshotPoint triggerPoint);

    private ITrackingSpan GetApplicableTo(KeyValuePair<SnapshotSpan, List<Completion>> completionResult) =>
        //TODO: double check if this logic is useful, but if this is enabled, it is impossible to change an existing entry using completion
        //var applicableToText = completionResult.Key.GetText();
        //// if the full insertion text has been typed in already, we skip
        //if (applicableToText.Length > 0 && completionResult.Value.Any(c => applicableToText.StartsWith(c.InsertionText)))
        //    return null;
        _buffer.CurrentSnapshot.CreateTrackingSpan(completionResult.Key, SpanTrackingMode.EdgeInclusive);
}
