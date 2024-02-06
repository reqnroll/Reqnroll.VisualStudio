using Reqnroll.VisualStudio.Discovery.TagExpressions;

namespace Reqnroll.VisualStudio.Discovery;

public class Scope
{
    public ITagExpression? Tag { get; set; }
    public string? FeatureTitle { get; set; }
    public string? ScenarioTitle { get; set; }

    public override string ToString()
    {
        var result = Tag?.ToString() ?? "";
        if (FeatureTitle != null)
        {
            result = result.Length > 0 ? result + ", " : result;
            result = $"{result}Feature='{FeatureTitle}'";
        }
        if (ScenarioTitle != null)
        {
            result = result.Length > 0 ? result + ", " : result;
            result = $"{result}Scenario='{ScenarioTitle}'";
        }
        return result;
    }
}
