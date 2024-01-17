namespace Reqnroll.VisualStudio.ReqnrollConnector.V2.Tests;

public class DynamicRuntimePluginLoaderFactoryTests
{
    [Fact]
    public void DynamicRuntimePluginLoaderFactoryCreatesTheType()
    {
        var dynamicRuntimePluginLoader = new DynamicRuntimePluginLoaderFactory();

        Type t = dynamicRuntimePluginLoader.Create();
        t.Should().BeAssignableTo<IRuntimePluginLoader_3_0_220>();

        var instance = Activator.CreateInstance(t, default(TestAssemblyLoadContext)) as IRuntimePluginLoader_3_0_220;

        instance
            .Invoking(i => i!.LoadPlugin("pluginAssemblyName", null!))
            .Should()
            .Throw<ReqnrollException>()
            .WithMessage("Unable to load plugin: pluginAssemblyName.*");
    }
}
