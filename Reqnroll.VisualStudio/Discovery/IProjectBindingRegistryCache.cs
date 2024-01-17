namespace Reqnroll.VisualStudio.Discovery;

public interface IProjectBindingRegistryCache
{
    ProjectBindingRegistry Value { get; }
    event EventHandler<EventArgs> Changed;

    Task Update(Func<ProjectBindingRegistry, ProjectBindingRegistry> updateFunc);
    Task Update(Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>> updateTask);
    Task<ProjectBindingRegistry> GetLatest();
}
