#nullable disable
namespace Reqnroll.VisualStudio.Discovery;

public class ProjectHookBinding : ProjectBinding
{
    public const int DefaultHookOrder = 10000;

    public HookType HookType { get; }
    public int HookOrder { get; }

    public ProjectHookBinding(ProjectBindingImplementation implementation, Scope scope, HookType hookType, int? hookOrder) 
        : base(implementation, scope)
    {
        HookType = hookType;
        HookOrder = hookOrder ?? DefaultHookOrder;
    }

    public bool Match(Scenario scenario, IGherkinDocumentContext context)
    {
        if (!MatchScope(context))
            return false;

        return true;
    }

    public override string ToString() => 
        Scope == null ? $"[{HookType}]: {Implementation}" : $"[{HookType}({Scope})]: {Implementation}";
}