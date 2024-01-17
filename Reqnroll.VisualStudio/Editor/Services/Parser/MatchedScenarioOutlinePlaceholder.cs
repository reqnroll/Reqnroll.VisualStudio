using System;
using System.Linq;

namespace Reqnroll.VisualStudio.Editor.Services.Parser;

public struct MatchedScenarioOutlinePlaceholder
{
    private readonly Match _match;

    public int Index => _match.Index;
    public int Length => _match.Length;
    public string Value => _match.Value;
    public string Name => _match.Groups["name"].Value;

    public MatchedScenarioOutlinePlaceholder(Match match)
    {
        _match = match;
    }

    private static readonly Regex ScenarioOutlineParamRe = new(@"\<(?<name>[^\>]+)\>");

    public static IEnumerable<MatchedScenarioOutlinePlaceholder> MatchScenarioOutlinePlaceholders(Step step)
    {
        return ScenarioOutlineParamRe.Matches(step.Text).Cast<Match>()
            .Select(m => new MatchedScenarioOutlinePlaceholder(m));
    }

    public static string ReplaceScenarioOutlinePlaceholders(Step step,
        Func<MatchedScenarioOutlinePlaceholder, string> replace)
    {
        return ScenarioOutlineParamRe.Replace(step.Text, m => replace(new MatchedScenarioOutlinePlaceholder(m)));
    }
}
