#nullable enable
namespace Reqnroll.VisualStudio.Discovery;

public interface IDiscoveryService : IDisposable
{
    IProjectBindingRegistryCache BindingRegistryCache { get; }
    event EventHandler<EventArgs> WeakBindingRegistryChanged;
    void TriggerDiscovery(string callerMemberName = "?");
}
