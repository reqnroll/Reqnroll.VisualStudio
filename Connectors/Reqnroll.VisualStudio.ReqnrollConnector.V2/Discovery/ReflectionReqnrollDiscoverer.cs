#nullable disable

namespace Reqnroll.VisualStudio.ReqnrollConnector.Discovery;

internal class ReflectionReqnrollDiscoverer : IReqnrollDiscoverer
{
    private readonly object _discovererObj;

    public ReflectionReqnrollDiscoverer(AssemblyLoadContext loadContext, Type discovererType)
    {
        var discovererAssembly = loadContext.LoadFromAssemblyPath(discovererType.Assembly.Location);
        // ReSharper disable once AssignNullToNotNullAttribute
        var discovererRemoteType = discovererAssembly.GetType(discovererType.FullName);
        _discovererObj = Activator.CreateInstance(discovererRemoteType, loadContext);
    }

    public string Discover(Assembly testAssembly, string testAssemblyPath, string configFilePath)
    {
        return _discovererObj.ReflectionCallMethod<string>(nameof(Discover),
            new[] {typeof(Assembly), typeof(string), typeof(string)},
            testAssembly, testAssemblyPath, configFilePath);
    }

    public void Dispose()
    {
        _discovererObj.ReflectionCallMethod<object>(nameof(Dispose));
    }
}
