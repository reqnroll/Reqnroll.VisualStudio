#nullable disable
using Reqnroll.VisualStudio.Discovery.TagExpressions;

namespace Reqnroll.VisualStudio.Discovery;

public class Scope
{
    public ITagExpression Tag { get; set; }
    public string FeatureTitle { get; set; }
    public string ScenarioTitle { get; set; }
}
