#nullable disable
namespace Reqnroll.VisualStudio.Editor.Commands;

public class RenameStepCommandContext
{
    public RenameStepCommandContext(IIdeScope ideScope, ITaggerProvider taggerProvider)
    {
        IdeScope = ideScope;
        TaggerProvider = taggerProvider;
        Issues = new List<Problem>();
    }

    public SnapshotPoint TriggerPointOfStepDefinitionClass { get; set; }
    public IIdeScope IdeScope { get; }
    public ITaggerProvider TaggerProvider { get; }
    public List<Problem> Issues { get; }
    public ITextBuffer TextBufferOfStepDefinitionClass { get; set; }
    public IProjectScope ProjectOfStepDefinitionClass { get; set; }
    public IStepDefinitionExpressionAnalyzer StepDefinitionExpressionAnalyzer { get; set; }
    [CanBeNull] public MethodDeclarationSyntax Method { get; set; }

    public IProjectScope[] ReqnrollTestProjectsWithFeatureFiles { get; set; }
    public ProjectStepDefinitionBinding StepDefinitionBinding { get; set; }
    public IProjectScope StepDefinitionProjectScope { get; set; }

    public string OriginalExpression => StepDefinitionBinding.Expression;
    public AnalyzedStepDefinitionExpression AnalyzedOriginalExpression { get; set; }
    public string UpdatedExpression { get; set; }
    public AnalyzedStepDefinitionExpression AnalyzedUpdatedExpression { get; set; }
    public bool IsErroneous => Issues.Any(issue => issue.Kind == Problem.ProblemKind.Critical);

    public void AddCriticalProblem(string description)
    {
        AddProblem(Problem.ProblemKind.Critical, description);
    }

    public void AddNotificationProblem(string description)
    {
        AddProblem(Problem.ProblemKind.Notification, description);
    }

    public void AddProblem(Problem.ProblemKind kind, string description)
    {
        Issues.Add(new Problem(kind, description));
    }
}
