namespace Reqnroll.VisualStudio.ProjectSystem.Actions;

public interface IBindingNavigation
{
    Task<bool> NavigateToBindingAsync(ProjectBindingImplementation implementation, IProjectScope projectScope);
}
