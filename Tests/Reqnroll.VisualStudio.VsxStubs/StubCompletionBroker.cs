#nullable disable
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Reqnroll.VisualStudio.VsxStubs;

public class StubCompletionBroker : ICompletionBroker
{
    private readonly ICompletionSource _completionSource;
    private ICompletionSession _completionSession;

    public ObservableCollection<CompletionSet> CompletionSets = new();

    public StubCompletionBroker(ICompletionSource completionSource)
    {
        _completionSource = completionSource;
    }

    public IEnumerable<Completion> Completions => CompletionSets.SelectMany(cs => cs.Completions);

    public ICompletionSession TriggerCompletion(ITextView textView)
    {
        CompletionSets.Clear();
        _completionSession = new StubCompletionSession(textView, CompletionSets);
        _completionSource.AugmentCompletionSession(_completionSession, CompletionSets);
        return _completionSession;
    }

    public bool IsCompletionActive(ITextView textView) => _completionSession != null;

    public ReadOnlyCollection<ICompletionSession> GetSessions(ITextView textView)
    {
        if (_completionSession == null)
            return new ReadOnlyCollection<ICompletionSession>(new List<ICompletionSession>());
        return new ReadOnlyCollection<ICompletionSession>(new List<ICompletionSession> {_completionSession});
    }

    public ICompletionSession TriggerCompletion(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret) =>
        throw new NotSupportedException();

    public ICompletionSession
        CreateCompletionSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret) =>
        throw new NotSupportedException();

    public void DismissAllSessions(ITextView textView)
    {
        throw new NotSupportedException();
    }
}
