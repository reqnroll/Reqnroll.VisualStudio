using Cucumber.TagExpressions;

namespace Reqnroll.VisualStudio.Discovery.TagExpressions;

public class ReqnrollTagExpression : ITagExpression
{
    public ReqnrollTagExpression(ITagExpression inner, string tagExpressionText)
    {
        TagExpressionText = tagExpressionText;
        _inner = inner;
    }
    public string TagExpressionText { get; }

    private ITagExpression _inner;

    public override string ToString()
    {
        return _inner.ToString();
    }

    public virtual bool Evaluate(IEnumerable<string> inputs)
    {
        return _inner.Evaluate(inputs);
    }
}
