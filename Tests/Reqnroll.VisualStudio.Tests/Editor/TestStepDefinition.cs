#nullable disable
namespace Reqnroll.VisualStudio.Tests.Editor;

public class TestStepDefinition : StepDefinition
{
    public static TestStepDefinition Void = new() {IsVoid = true};
    private SyntaxToken _testExpression;
    private SourceLocation _testSourceLocation;

    public bool IsVoid { get; private set; }

    public string AttributeName { get; set; }

    public SyntaxToken TestExpression
    {
        get => _testExpression;
        set
        {
            _testExpression = value;
            Regex = value.Text == string.Empty
                ? "^(?i)I[^\\w\\p{Sc}]*(?!(?<=-)\\d)Press[^\\w\\p{Sc}]*(?!(?<=-)\\d)Add[^\\w\\p{Sc}]*(?!(?<=-)\\d)$"
                : $"^{_testExpression.ValueText}$";
        }
    }

    public SourceLocation TestSourceLocation
    {
        get => _testSourceLocation;
        set
        {
            _testSourceLocation = value;
            SourceLocation =
                $"{_testSourceLocation.SourceFile}|{_testSourceLocation.SourceFileLine}|{_testSourceLocation.SourceFileColumn}";
        }
    }

    public string PopupLabel => $"[{Type}({TestExpression.ValueText})]: {Method}";
}
