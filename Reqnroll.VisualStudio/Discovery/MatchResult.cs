#nullable enable

namespace Reqnroll.VisualStudio.Discovery;

public class MatchResult
{
    public static readonly MatchResult NoMatch = new(Array.Empty<MatchResultItem>(), Array.Empty<string>());

    private MatchResult([ValidatedNotNull] MatchResultItem[] items, [ValidatedNotNull] string[] errors)
    {
        Items = items;
        Errors = errors;
    }

    public MatchResultItem[] Items { get; }

    public string[] Errors { get; }
    public bool HasErrors => Errors.Any();

    public bool HasUndefined =>
        Items.Any(m => m.Type == MatchResultType.Undefined);

    public bool HasDefined =>
        Items.Any(m => m.Type == MatchResultType.Defined);

    public bool HasAmbiguous =>
        Items.Any(m => m.Type == MatchResultType.Ambiguous);

    public bool HasMultipleMatches =>
        Items.Length > 1;

    public bool HasSingleMatch =>
        Items.Length == 1;

    public override string ToString()
    {
        return string.Join(Environment.NewLine, Items.Select(sd => sd.ToString()));
    }

    public string? GetErrorMessage()
    {
        if (!HasErrors)
            return null;
        return string.Join(Environment.NewLine, Errors);
    }

    public static MatchResult CreateMultiMatch(MatchResultItem[] items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        var errors = items.SelectMany(m => m.Errors);
        if (items.Any(m => m.Type == MatchResultType.Ambiguous))
        {
            var ambiguousMatches = items.Where(m => m.Type == MatchResultType.Ambiguous);
            var ambiguousErrorMessage =
                $"Ambiguous steps: {Environment.NewLine}{string.Join(Environment.NewLine, ambiguousMatches.Select(sd => sd.MatchedStepDefinition.ToString()))}";
            errors = errors.Concat(new[] {ambiguousErrorMessage});
        }

        return new MatchResult(items, errors.ToArray());
    }
}
