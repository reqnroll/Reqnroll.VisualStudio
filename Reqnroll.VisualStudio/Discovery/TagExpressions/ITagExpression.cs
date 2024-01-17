namespace Reqnroll.VisualStudio.Discovery.TagExpressions;

public interface ITagExpression
{
    bool Evaluate(IEnumerable<string> variables);
}
