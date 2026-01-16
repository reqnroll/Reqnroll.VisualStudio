using Cucumber.TagExpressions;

namespace Reqnroll.VisualStudio.Discovery;

public class Scope
{
    public ITagExpression? Tag { get; set; }
    public string? FeatureTitle { get; set; }
    public string? ScenarioTitle { get; set; }
    public string? Error { get; set; }

    public bool IsValid => Error == null;

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
        if (Error != null)
        {
            result = result.Length > 0 ? result + ", " : result;
            result = $"{result}Error='{Error}'";
        }
        return result;
    }
}
