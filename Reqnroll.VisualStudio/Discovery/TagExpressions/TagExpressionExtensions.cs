using Cucumber.TagExpressions;
using System.Linq;

namespace Reqnroll.VisualStudio.Discovery.TagExpressions;

public static class TagExpressionExtensions
{
    public static bool EvaluateWithDefault(this ITagExpression tagExpression, IEnumerable<string> tags,
        bool defaultValue) => tagExpression?.Evaluate(tags) ?? defaultValue;

    public static bool EvaluateWithDefault(this ITagExpression tagExpression, IEnumerable<Tag> tags, bool defaultValue)
    {
        return tagExpression?.Evaluate(tags.Select(t => t.Name)) ?? defaultValue;
    }
}
