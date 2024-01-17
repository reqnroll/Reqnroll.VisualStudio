#nullable enable
namespace Reqnroll.VisualStudio.VsxStubs;

public class StubProjectBindingRegistryCache : Mock<IProjectBindingRegistryCache>, IProjectBindingRegistryCache
{
    public StubProjectBindingRegistryCache() : base(MockBehavior.Strict)
    {
        Value = ProjectBindingRegistry.Invalid;
        Setup(c => c.Update(It.IsAny<Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>>>()))
            .Returns(async (Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>> updateFunc) =>
            {
                Value = await updateFunc(Value);
                return Task.FromResult(Value);
            });
    }

    public bool Processing { get; }

    public event EventHandler<EventArgs>? Changed;

    public Task Update(Func<ProjectBindingRegistry, ProjectBindingRegistry> updateFunc)
        => Update(registry => Task.FromResult(updateFunc(registry)));

    public Task Update(Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>> updateTask)
        => Object.Update(updateTask);

    public ProjectBindingRegistry Value { get; private set; }
    public Task<ProjectBindingRegistry> GetLatest() => throw new NotImplementedException();
}
