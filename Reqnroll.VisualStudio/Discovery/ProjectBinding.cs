#nullable disable
namespace Reqnroll.VisualStudio.Discovery;

public class ProjectBinding
{
    public Scope Scope { get; }
    public ProjectBindingImplementation Implementation { get; }
    public virtual bool IsValid => Error == null && Scope?.IsValid != false;
    public string Error { get; }

    public ProjectBinding(ProjectBindingImplementation implementation, Scope scope, string error = null)
    {
        Implementation = implementation;
        Scope = scope;
        Error = error;
    }

    protected bool MatchScope(IGherkinDocumentContext context)
    {
        if (Scope != null)
        {
            if (Scope.Tag != null && !Scope.Tag.Evaluate(context.GetTagNames()))
                return false;
            if (Scope.FeatureTitle != null && context.AncestorOrSelfNode<Feature>()?.Name != Scope.FeatureTitle)
                return false;
            if (Scope.ScenarioTitle != null && context.AncestorOrSelfNode<Scenario>()?.Name != Scope.ScenarioTitle)
                return false;
        }

        return true;
    }
}