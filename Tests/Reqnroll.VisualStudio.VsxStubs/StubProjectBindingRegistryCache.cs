#nullable enable
using NSubstitute;

namespace Reqnroll.VisualStudio.VsxStubs;

public class StubProjectBindingRegistryCache : IProjectBindingRegistryCache
{
    public IProjectBindingRegistryCache Substitute { get; }

    public StubProjectBindingRegistryCache()
    {
        Substitute = NSubstitute.Substitute.For<IProjectBindingRegistryCache>();

        Value = ProjectBindingRegistry.Invalid;
        Substitute.Update(Arg.Any<Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>>>())
                   .Returns(async callInfo =>
                   {
                       var updateFunc = callInfo.Arg<Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>>>();
                       Value = await updateFunc(Value);
                   });
    }

    public event EventHandler<EventArgs>? Changed;

    public Task Update(Func<ProjectBindingRegistry, ProjectBindingRegistry> updateFunc)
        => Update(registry => Task.FromResult(updateFunc(registry)));

    public Task Update(Func<ProjectBindingRegistry, Task<ProjectBindingRegistry>> updateTask)
        => Substitute.Update(updateTask);

    public ProjectBindingRegistry Value { get; private set; }
    public Task<ProjectBindingRegistry> GetLatest() => throw new NotImplementedException();
}
