namespace Reqnroll.VisualStudio.Discovery;
public class HookMatchResult
{
    public ProjectHookBinding[] Items { get; }

    public bool HasHooks => Items.Length > 0;

    public HookMatchResult(ProjectHookBinding[] items)
    {
        Items = items;
    }
}
