#nullable disable

namespace Reqnroll.VisualStudio.Editor.Completions.Infrastructure;

public class CompletionSessionManager
{
    private const int MinimumCharactersForAutoCompletion = 3;
    private readonly ICompletionBroker _completionBroker;

    protected readonly IWpfTextView _textView;
    private ICompletionSession _currentSession;

    public CompletionSessionManager(IWpfTextView textView, ICompletionBroker completionBroker)
    {
        _textView = textView;
        _completionBroker = completionBroker;
    }

    public bool IsActive => _currentSession != null;

    public bool TriggerCompletion()
    {
        if (!IsActive)
        {
            if (!_completionBroker.IsCompletionActive(_textView))
            {
                _currentSession = _completionBroker.TriggerCompletion(_textView);
                if (IsActive)
                {
                    _currentSession.Dismissed += CurrentSessionOnDismissed;
                    _currentSession.Committed += CurrentSessionOnDismissed;
                }
            }
            else
            {
                _currentSession = _completionBroker.GetSessions(_textView)[0];
            }
        }

        if (IsActive && _currentSession.SelectedCompletionSet != null)
        {
            var completionSet = _currentSession.SelectedCompletionSet;
            if (completionSet.SelectionStatus.IsSelected &&
                completionSet.SelectionStatus.IsUnique &&
                completionSet.ApplicableTo.GetSpan(_textView.TextBuffer.CurrentSnapshot).Length >=
                MinimumCharactersForAutoCompletion)
            {
                // if at least 3 characters are typed in and the selection is unique we auto complete the selection
                _currentSession.Commit();
                _currentSession = null;
            }
        }

        if (IsActive)
        {
            //NOTE: call _currentSession.Filter() to narrow the list to the applicable items only
        }

        return true;
    }

    private void CurrentSessionOnDismissed(object sender, EventArgs eventArgs)
    {
        _currentSession.Dismissed -= CurrentSessionOnDismissed;
        _currentSession.Committed -= CurrentSessionOnDismissed;
        _currentSession = null;
    }

    public bool Filter()
    {
        if (!IsActive || _currentSession.SelectedCompletionSet == null)
            return false;

        var completionSet = _currentSession.SelectedCompletionSet;

        completionSet.Filter();
        completionSet.SelectBestMatch();
        completionSet.Recalculate();

        if (completionSet.SelectionStatus.IsSelected &&
            completionSet.SelectionStatus.IsUnique &&
            completionSet.ApplicableTo.GetSpan(_textView.TextBuffer.CurrentSnapshot).GetText().Equals(
                completionSet.SelectionStatus.Completion.InsertionText, StringComparison.CurrentCultureIgnoreCase))
        {
            _currentSession.Commit();
            _currentSession = null;
        }

        return true;
    }

    public bool Complete(bool force)
    {
        if (!IsActive || _currentSession.SelectedCompletionSet == null)
            return false;

        if (!_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected && !force)
        {
            _currentSession.Dismiss();
            return false;
        }

        _currentSession.Commit();
        return true;
    }

    public bool Cancel()
    {
        if (!IsActive)
            return false;

        _currentSession.Dismiss();
        return true;
    }
}
