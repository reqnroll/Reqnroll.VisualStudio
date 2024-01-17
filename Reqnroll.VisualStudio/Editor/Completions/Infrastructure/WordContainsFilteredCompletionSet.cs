using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Editor.Completions.Infrastructure;

public class WordContainsFilteredCompletionSet : ExtendableCompletionSet
{
    public WordContainsFilteredCompletionSet(string moniker, string displayName, ITrackingSpan applicableTo,
        IEnumerable<Completion> completions, IEnumerable<Completion> completionBuilders) : base(moniker, displayName,
        applicableTo, completions, completionBuilders)
    {
    }

    protected override bool DoesTextMatch(string text, string filterText, bool caseSensitive)
    {
        var comparison = caseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
        if (ContainsText(text, filterText, comparison))
            return true; // normal contains

        var filterWords = Regex.Split(filterText, @"\W+").Where(w => !string.IsNullOrWhiteSpace(w)).ToArray();
        if (filterWords.Length <= 1)
            return false; // there are no multiple words

        foreach (var filterWord in filterWords)
            if (!ContainsText(text, filterWord, comparison))
                return false;

        return true;
    }

    private bool ContainsText(string text, string filterText, StringComparison comparison) =>
        text.IndexOf(filterText, comparison) >= 0;
}
