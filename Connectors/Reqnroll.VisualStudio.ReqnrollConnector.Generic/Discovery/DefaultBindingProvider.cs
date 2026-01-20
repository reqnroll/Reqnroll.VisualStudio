using Reqnroll.Bindings.Provider.Data;
using ReqnrollConnector.Utils;

namespace ReqnrollConnector.Discovery;

public interface IBindingProvider
{
    BindingData DiscoverBindings(TestAssemblyLoadContext testAssemblyContext, Assembly testAssembly, string? configFileContent, ILogger log);
}

/// <summary>
/// The default discovery command that should work for all Reqnroll versions.
/// Once the discovery interface of Reqnroll changes, another implementation can be created.
/// </summary>
public class DefaultBindingProvider: IBindingProvider
{
    public BindingData DiscoverBindings(TestAssemblyLoadContext testAssemblyContext, Assembly testAssembly, string? configFileContent, ILogger log)
    {
        string bindingJson = GetBindingsJson(testAssemblyContext, testAssembly, configFileContent);
        var bindingData = DeserializeBindingData(bindingJson, log);
        return bindingData;
    }

    /// <summary>
    /// Calls the BindingProviderService.DiscoverBindings via reflection to get the bindings json.
    /// The result is a serialized json string representing the discovered bindings represented by the <see cref="BindingData"/> class.
    /// </summary>
    private string GetBindingsJson(TestAssemblyLoadContext testAssemblyContext, Assembly testAssembly, string? configFileContent)
    {
        var reqnrollAssembly = testAssemblyContext.LoadFromAssemblyName(new AssemblyName("Reqnroll"));
        var bindingProviderServiceType = reqnrollAssembly.GetType("Reqnroll.Bindings.Provider.BindingProviderService", true)!;
        var bindingJson = bindingProviderServiceType.ReflectionCallStaticMethod<string>("DiscoverBindings", new[] { typeof(Assembly), typeof(string) }, testAssembly, configFileContent);
        return bindingJson;
    }

    /// <summary>
    /// Deserializes the bindings json into the <see cref="BindingData"/> object.
    /// Currently, the <see cref="BindingData"/> class structure is compatible with the
    /// binding data returned by any Reqnroll version, therefore we can directly deserialize
    /// the returned JSON to <see cref="BindingData"/>.
    /// Later, we might need to implement version-specific deserialization logic here.
    /// </summary>
    private BindingData DeserializeBindingData(string bindingJson, ILogger log)
    {
        var bindingData = JsonSerialization.DeserializeObjectDefaultCase<BindingData>(bindingJson, log);
        return bindingData ?? new BindingData();
    }
}